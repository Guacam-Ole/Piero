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
    private readonly object _processLock = new();
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
        targetDirectory = GetPath(Path.GetDirectoryName(sourceFilename), targetDirectory);
        var targetFile = Path.Combine(targetDirectory, filenameWithoutExtension);
        if (!Directory.Exists(targetDirectory)) return false;
        var existingConversion = Directory.GetFiles(targetDirectory, $"{filenameWithoutExtension}.*").FirstOrDefault();
        return existingConversion != null;
    }

    public static string GetPath(string sourceDirectory, string targetDirectory)
    {
        if (targetDirectory.StartsWith("./")) targetDirectory = Path.Combine(sourceDirectory, targetDirectory);
        return targetDirectory;
    }

    public async Task<bool> StartConversion(string sourceDirectory, VideoFile fileToConvert,
        Config.FfMpegConfig config, bool isMainConversion)
    {
        var targetDirectory =
            GetPath(
                sourceDirectory, isMainConversion ? _config.VideoPath : _config.ProxyPath
            );
        var sourceFilename = fileToConvert.FullName;

        try
        {
            if (!Directory.Exists(targetDirectory)) Directory.CreateDirectory(targetDirectory);
            var filenameWithoutExtension = Path.GetFileNameWithoutExtension(sourceFilename);
            var sourceFile = Path.Combine(sourceDirectory, sourceFilename);

            var conversionParameters = config.Command.Replace("[INPUT]", sourceFile)
                .Replace("[OUTPUT]", Path.Combine(targetDirectory, filenameWithoutExtension));

            conversionParameters = $"{_config.FfmpegPrefix} {conversionParameters}";

            ConversionInfo conversionInfo = new ConversionInfo
            {
                IsMainConversion = isMainConversion,
                VideoFile = fileToConvert,
                FolderName = sourceDirectory
            };
            
            lock (_processLock)
            {
                _runningConversions.Add(conversionInfo);
            }
            
            var ffmpegProcess = new Process();

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

            lock (_processLock)
            {
                conversionInfo.Id = ffmpegProcess.Id;
            }
            await ffmpegProcess.WaitForExitAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot convert to '{directory}'. Aborting Conversion", targetDirectory);
            return false;
        }
    }

    private ConversionInfo? GetConversionInfo(int id)
    {
        lock (_processLock)
        {
            return _runningConversions.FirstOrDefault(q => q.Id == id);
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
        if (line.Contains("Error"))
        {
            _logger.LogError(line);
            var conversionInfo = GetConversionInfo(process.Id);
            ProgressChanged?.Invoke(sender, new FfmpegEventArgs(conversionInfo, 0) { IsError = true});
            return;
        }
        var duration = GetDuration(process.Id, line);
        var position = GetPosition(process.Id, line);
        if (duration == null && position == null) _logger.LogDebug(line);
        CalculateProgress(process.Id);
    }

    private TimeSpan? GetDuration(int id, string ffmpegOutput)
    {
        var conversionInfo = GetConversionInfo(id);
        if (conversionInfo == null) return null;
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
        if (conversionInfo == null) return position;
        conversionInfo.Position = position;
        UpdateConversionState(conversionInfo);
        return position;
    }

    private double CalculateProgress(int id)
    {
        var conversionInfo = GetConversionInfo(id);
        if (conversionInfo == null) return 0;
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
        ProgressChanged?.Invoke(this, new FfmpegEventArgs(conversionInfo!, 100));
        RemoveConversionInfo(process.Id);

        _logger.LogInformation("Conversion finished");
    }

    public class ConversionInfo
    {
        public int Id { get; set; }
        public TimeSpan Duration { get; set; }
        public TimeSpan Position { get; set; }
        public VideoFile VideoFile { get; init; } = new();
        public bool IsMainConversion { get; init; }
        public string FolderName { get; init; } = string.Empty;
    }
}