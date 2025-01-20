using System;
using System.Diagnostics;
using Avalonia.Media;

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

    public string FileName { get; set; }
    public string FilePath { get; set; }
    public ConversionState MainVideoConversionState { get; set; }
    public ConversionState ProxyConversionState { get; set; }
    public DateTime FileDate { get; set; }





  
}