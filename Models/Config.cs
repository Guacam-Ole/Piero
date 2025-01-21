using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;

namespace Piero.Models;

public class Config
{
    public bool ShowHeader { get; set; } = true;
    public int ConversionIndex { get; set; } 
    public int ProxyIndex { get; set; } 
    public required string FfmpegPath { get; set; }
    public required string ProxyPath { get; set; }
    public required string VideoPath { get; set; }
    public List<string> Extensions { get; set; } = [];
    public string LogFile { get; set; } = "piero.log";
    

    public List<string> Paths { get; set; } = [];

    public List<FfMpegConfig> FfmpegConfigs { get; set; } = [];

    public bool AddPath(string path)
    {
        if (Paths.Contains(path)) return false;
        Paths.Add(path);
        return true;
    }

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