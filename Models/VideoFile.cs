namespace Piero.Models;

public class VideoFile
{
    public enum ConversionState
    {
        Pending,
        Converting,
        Converted,
        Error
    }

    public string FullName { get; set; } = string.Empty;
    public string HumanReadableFileSize { get; set; } = string.Empty;
    public ConversionState MainVideoConversionState { get; set; }
    public ConversionState ProxyConversionState { get; set; }
    public int MainProgress { get; set; }
    public int ProxyProgress { get; set; }
}