using System;
using System.Diagnostics;
using Avalonia.Media;

namespace Piero.Models;

public class VideoFile
{
    public enum ConversionState
    {
        Unknown,
        Converting,
        Converted,
        Error
    }

    public string FileName { get; set; }
    public string FilePath { get; set; }
    public ConversionState MainVideoConversionState { get; set; }
    public ConversionState ProxyConversionState { get; set; }
    public DateTime FileDate { get; set; }
    public DateTime? MainVideoConversionDate { get; set; }
    public DateTime? PreviewConversionDate { get; set; }


    private string GetProgressColor(ConversionState state)
    {
        return state switch
        {
            ConversionState.Converting => "LightGray",
            ConversionState.Converted => "LightGreen",
            ConversionState.Error => "Orange",
            ConversionState.Unknown => "LightGray",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private string GetProgressIcon(ConversionState state)
    {
        return state switch
        {
            ConversionState.Converting => "\u231b\ufe0f",
            ConversionState.Converted => "\ud83d\udc4d\ufe0f",
            ConversionState.Error => "\ud83d\udc4e\ufe0f",
            ConversionState.Unknown => "\u2753\ufe0f",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public string MainProgressColor => GetProgressColor(MainVideoConversionState);

    public string ProxyProgressColor => GetProgressColor(ProxyConversionState);

    public string MainProgressIcon => GetProgressIcon(MainVideoConversionState);

    public string ProxyProgressIcon => GetProgressIcon(ProxyConversionState);
}