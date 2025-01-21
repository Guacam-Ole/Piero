using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Primitives;
using Microsoft.Extensions.Logging;
using Piero.Models;

namespace Piero;

public class Converter
{
    private readonly ILogger<Converter> _logger;
    private readonly Config _config;

    public Converter(ILogger<Converter> logger, Config config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task StartConversion(string sourceDirectory, string targetDirectory, string sourceFilename, string command)
    {
        if (targetDirectory.StartsWith("./")) targetDirectory = Path.Combine(sourceDirectory, targetDirectory);
        try
        {
            if (!Directory.Exists(targetDirectory)) Directory.CreateDirectory(targetDirectory);
            var filenameWithoutExtension = Path.GetFileNameWithoutExtension(sourceFilename);
            var sourceFile=Path.Combine(sourceDirectory, sourceFilename);
            
            var conversionParameters=command.Replace("[INPUT]",sourceFile).Replace("[OUTPUT}",Path.Combine(targetDirectory,filenameWithoutExtension));

            conversionParameters = $"-progress pipe:1 -y {conversionParameters} ";
            using var ffmpegProcess=new Process();
            ffmpegProcess.StartInfo.UseShellExecute = false;
            ffmpegProcess.StartInfo.CreateNoWindow = true;
            ffmpegProcess.StartInfo.RedirectStandardOutput = true;
            ffmpegProcess.StartInfo.FileName = _config.FfmpegPath;  
            ffmpegProcess.StartInfo.Arguments = conversionParameters;
            ffmpegProcess.StartInfo.RedirectStandardError=true;
            ffmpegProcess.StartInfo.RedirectStandardOutput=true;
            ffmpegProcess.EnableRaisingEvents = true;
            
            ffmpegProcess.Exited += FfmpegProcessOnExited;
            ffmpegProcess.ErrorDataReceived+= FfmpegProcessOnErrorDataReceived;
            ffmpegProcess.OutputDataReceived+= FfmpegProcessOnOutputDataReceived;
            ffmpegProcess.Start();
            ffmpegProcess.BeginErrorReadLine();
            ffmpegProcess.BeginOutputReadLine();
            
            await ffmpegProcess.WaitForExitAsync();
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot create directory '{directory}'. Aborting Conversion", targetDirectory);
        }
    }

    private void FfmpegProcessOnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        _logger.LogDebug(e.Data);
    }

    private void FfmpegProcessOnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        // Status is sent to error log weirdly enoug
       _logger.LogError("something went wrong when trying to convert file: {data}",e.Data);
    }

    private void FfmpegProcessOnExited(object? sender, EventArgs e)
    {
        _logger.LogInformation("Conversion finished");
    }
}