using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    private readonly List<ConversionInfo> _runningConversions = [];


    public Converter(ILogger<Converter> logger, Config config)
    {
        _logger = logger;
        _config = config;
    }

    public static bool TargetExists(string sourceFilename, string targetDirectory)
    {
        var filenameWithoutExtension = Path.GetFileNameWithoutExtension(sourceFilename);
        var targetFile = Path.Combine(targetDirectory, filenameWithoutExtension);
        return File.Exists(targetFile);
    }

    public async Task<bool> StartConversion(string sourceDirectory, VideoFile fileToConvert,
        string command, bool isMainConversion)
    {
        var targetDirectory = isMainConversion ? _config.VideoPath : _config.ProxyPath;
        var sourceFilename = fileToConvert.FullName;
        if (targetDirectory.StartsWith("./")) targetDirectory = Path.Combine(sourceDirectory, targetDirectory);
        try
        {
            if (!Directory.Exists(targetDirectory)) Directory.CreateDirectory(targetDirectory);
            var filenameWithoutExtension = Path.GetFileNameWithoutExtension(sourceFilename);
            var sourceFile = Path.Combine(sourceDirectory, sourceFilename);

            var conversionParameters = command.Replace("[INPUT]", sourceFile)
                .Replace("[OUTPUT]", Path.Combine(targetDirectory, filenameWithoutExtension));

            conversionParameters = $"{_config.FfmpegPrefix} {conversionParameters}";
            using var ffmpegProcess = new Process();
            lock (_processLock)
            {
                _runningConversions.Add(new ConversionInfo
                {
                    IsMainConversion = isMainConversion,
                    VideoFile = fileToConvert,
                    Id = ffmpegProcess.Id
                });
            }
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
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot create directory '{directory}'. Aborting Conversion", targetDirectory);
            return false;
        }
    }

    private ConversionInfo GetConversionInfo(int id)
    {
        lock (_processLock)
        {
            return _runningConversions.First(q => q.Id == id);
        }
    }

    private void RemoveConversionInfo(int id)
    {
        lock (_processLock)
        {
            _runningConversions.RemoveAll(q => q.Id == id);
        }
    }

    private void UpdateConversionState(ConversionInfo info)
    {
        lock (_processLock)
        {
            var conversion = _runningConversions.First(q => q.Id == info.Id);
            conversion.Duration = info.Duration;
            conversion.Position = info.Position;
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
        var conversionInfo = GetConversionInfo(id);
        if (conversionInfo.Duration != TimeSpan.Zero) return null; // Already received
        if (!ffmpegOutput.StartsWith("Duration:")) return null;
        var lengthStart = "Duration:".Length + 1;
        if (lengthStart < 0) return null;
        var lengthEnd = ffmpegOutput.IndexOf(',', lengthStart);
        if (lengthEnd < 0) return null;
        var lengthCaption = ffmpegOutput.Substring(lengthStart, lengthEnd - lengthStart).Trim();

        if (!TimeSpan.TryParse(lengthCaption, out var duration)) return null;
        conversionInfo.Duration = duration;
        UpdateConversionState(conversionInfo);
        return duration;
    }

    private TimeSpan? GetPosition(int id, string ffmpegOutput)
    {
        var timeStart = ffmpegOutput.IndexOf("time=", StringComparison.Ordinal);
        if (timeStart < 0) return null;
        timeStart += "time=".Length + 1;
        var timeEnd = ffmpegOutput.IndexOf(' ', timeStart + 1);
        if (timeEnd < 0) return null;
        var timeCaption = ffmpegOutput.Substring(timeStart, timeEnd - timeStart - 1).Trim();

        if (!TimeSpan.TryParse(timeCaption, out var position)) return null;
        var conversionInfo = GetConversionInfo(id);
        conversionInfo.Position = position;
        UpdateConversionState(conversionInfo);
        return position;
    }

    private double CalculateProgress(int id)
    {
        var conversionInfo = GetConversionInfo(id);
        TimeSpan duration;
        TimeSpan position;
        if (conversionInfo.Duration == TimeSpan.Zero || conversionInfo.Position == TimeSpan.Zero) return 0;

        if (conversionInfo.Position > conversionInfo.Duration) return 100;
        if (conversionInfo.Duration.TotalSeconds == 0) return 0;
        var newProgress = conversionInfo.Position.TotalSeconds * 100 / conversionInfo.Duration.TotalSeconds;

        ProgressChanged?.Invoke(this, new FfmpegEventArgs(conversionInfo, (int)newProgress));
        return newProgress;
    }

    private void FfmpegProcessOnExited(object? sender, EventArgs e)
    {
        var process = (Process)sender!;
        var conversionInfo = GetConversionInfo(process.Id);
        ProgressChanged?.Invoke(this, new FfmpegEventArgs(conversionInfo, 100));
        RemoveConversionInfo(process.Id);

        _logger.LogInformation("Conversion finished");
    }

    public class ConversionInfo
    {
        public int Id { get; set; }
        public TimeSpan Duration { get; set; }
        public TimeSpan Position { get; set; }
        public VideoFile VideoFile { get; set; }=new ();
        public bool IsMainConversion { get; set; }
    }
}