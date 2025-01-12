namespace Piero.Models;

public class Config
{
    public string StartupConversion { get; set; }
    public string StartupProxy { get; set; }
    public string FfmpegPath { get; set; }
    public string ProxyPath { get; set; }
    public string VideoPath { get; set; }   
    public string LogFile { get; set; }="piero.log";
}