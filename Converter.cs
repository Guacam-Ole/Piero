using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Piero.Models;

namespace Piero;

public class Converter
{
    private object _processLock = new object();
    public EventHandler<FfmpegEventArgs> ProgressChanged;
    private readonly ILogger<Converter> _logger;
    private readonly Config _config;
    private readonly Dictionary<int, TimeSpan> _durations = new();
    private readonly Dictionary<int, TimeSpan> _positions = new();


    public Converter(ILogger<Converter> logger, Config config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task StartConversion(string sourceDirectory, string targetDirectory, string sourceFilename,
        string command)
    {
        if (targetDirectory.StartsWith("./")) targetDirectory = Path.Combine(sourceDirectory, targetDirectory);
        try
        {
            if (!Directory.Exists(targetDirectory)) Directory.CreateDirectory(targetDirectory);
            var filenameWithoutExtension = Path.GetFileNameWithoutExtension(sourceFilename);
            var sourceFile = Path.Combine(sourceDirectory, sourceFilename);

            var conversionParameters = command.Replace("[INPUT]", sourceFile)
                .Replace("[OUTPUT}", Path.Combine(targetDirectory, filenameWithoutExtension));

            conversionParameters = $"{_config.FfmpegPrefix} {conversionParameters}";
            using var ffmpegProcess = new Process();
            ffmpegProcess.StartInfo.UseShellExecute = false;
            ffmpegProcess.StartInfo.CreateNoWindow = true;
            ffmpegProcess.StartInfo.RedirectStandardOutput = true;
            ffmpegProcess.StartInfo.FileName = _config.FfmpegPath;
            ffmpegProcess.StartInfo.Arguments = conversionParameters;
            ffmpegProcess.StartInfo.RedirectStandardError = true;
            ffmpegProcess.EnableRaisingEvents = true;

            ffmpegProcess.Exited += FfmpegProcessOnExited;
            ffmpegProcess.ErrorDataReceived += FfmpegProcessOnErrorDataReceived;
            ffmpegProcess.Start();
            ffmpegProcess.BeginErrorReadLine();

            await ffmpegProcess.WaitForExitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot create directory '{directory}'. Aborting Conversion", targetDirectory);
        }
    }

    private void FfmpegProcessOnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        // Weirdly, the error output is used for updates
        if (e.Data == null) return;
        var line = e.Data.Trim();
        var process = (Process)sender;
        var duration = GetDuration(process.Id, line);
        var position = GetPosition(process.Id, line);
        if (duration == null && position == null) _logger.LogDebug(line);
        var progress = CalculateProgress(process.Id);
        _logger.LogDebug("FfMpeg progress for {id}: {progress}%", process.Id, progress);
    }

    private TimeSpan? GetDuration(int id, string ffmpegOutput)
    {
        lock (_processLock)
        {
            if (_durations.ContainsKey(id)) return null; // Already received
            if (!ffmpegOutput.StartsWith("Duration:")) return null;
            var lengthStart = "Duration:".Length + 1;
            if (lengthStart < 0) return null;
            var lengthEnd = ffmpegOutput.IndexOf(',', lengthStart);
            if (lengthEnd < 0) return null;
            var lengthCaption = ffmpegOutput.Substring(lengthStart, lengthEnd - lengthStart).Trim();

            if (!TimeSpan.TryParse(lengthCaption, out var duration)) return null;
            _durations[id] = duration;
            return duration;
        }
    }

    private TimeSpan? GetPosition(int id, string ffmpegOutput)
    {
        lock (_processLock)
        {
            var timeStart = ffmpegOutput.IndexOf("time=", StringComparison.Ordinal);
            if (timeStart < 0) return null;
            timeStart += "time=".Length + 1;
            var timeEnd = ffmpegOutput.IndexOf(' ', timeStart + 1);
            if (timeEnd < 0) return null;
            var timeCaption = ffmpegOutput.Substring(timeStart, timeEnd - timeStart - 1).Trim();

            if (!TimeSpan.TryParse(timeCaption, out var position)) return null;
            _positions[id] = position;
            return position;
        }
    }

    private double CalculateProgress(int id)
    {
        TimeSpan duration;
        TimeSpan position;
        lock (_processLock)
        {
            if (!_durations.TryGetValue(id, out  duration) ||
                !_positions.TryGetValue(id, out  position)) return 0;
        }

        if (position > duration) return 100;
            if (duration.TotalSeconds == 0) return 0;
            var newProgress = position.TotalSeconds * 100 / duration.TotalSeconds;
        

        ProgressChanged?.Invoke(this, new FfmpegEventArgs(id, _positions[id], (int)newProgress));
        return newProgress;
    }

    private void FfmpegProcessOnExited(object? sender, EventArgs e)
    {
        var process = (Process)sender!;
        lock (_processLock)
        {
            _positions.Remove(process.Id);
            _durations.Remove(process.Id);
        }

        _logger.LogInformation("Conversion finished");
    }
}