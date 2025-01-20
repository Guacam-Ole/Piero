using System.Collections.Generic;

namespace Piero.Models;

public class Config
{
    public int ConversionIndex { get; set; } = 1;
    public int ProxyIndex { get; set; } = 2;
    public string FfmpegPath { get; set; }
    public string ProxyPath { get; set; }
    public string VideoPath { get; set; }
    public string LogFile { get; set; } = "piero.log";

    public List<FfMpegConfig> FfmpegConfigs { get; set; } =
    [
        new("No Conversion", "ffmpeg.exe"),
        new("4K, DTS 5.1", "ffmpeg.exe"),
        new("1080p", "ffmpeg.exe"),
        new("720p", "ffmpeg.exe")
    ];

    public class FfMpegConfig
    {
        public FfMpegConfig(string label, string command)
        {
            Label = label;
            Command = command;
        }

        public string Command { get; set; }
        public string Label { get; set; }
    }
}