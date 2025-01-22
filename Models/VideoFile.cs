using System;
using System.IO;

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

    public string FullName { get; set; }
    public ConversionState MainVideoConversionState { get; set; }
    public ConversionState ProxyConversionState { get; set; }
    public int MainProgress { get; set; }
    public int ProxyProgress { get; set; }
}