using System;
using System.Collections.Generic;
using System.Linq;

namespace Piero.Models;

public class FolderInfo
{
    public VideoFile.ConversionState ProxyState
    {
        get
        {
            if (FilesToConvert == null) return VideoFile.ConversionState.Error;
            if (FilesToConvert.Any(file => file.ProxyConversionState == VideoFile.ConversionState.Error))
                return VideoFile.ConversionState.Error;
            if (TotalFiles == ProxiedFiles) return VideoFile.ConversionState.Converted;
            return FilesToConvert.Any(file => file.ProxyConversionState == VideoFile.ConversionState.Converting)
                ? VideoFile.ConversionState.Converting
                : VideoFile.ConversionState.Pending;
        }
    }

    public VideoFile.ConversionState ConvertedState
    {
        get
        {
            if (FilesToConvert.Any(file => file.MainVideoConversionState == VideoFile.ConversionState.Error))
                return VideoFile.ConversionState.Error;
            if (TotalFiles == ConvertedFiles) return VideoFile.ConversionState.Converted;
            return FilesToConvert.Any(file => file.MainVideoConversionState == VideoFile.ConversionState.Converting)
                ? VideoFile.ConversionState.Converting
                : VideoFile.ConversionState.Pending;
        }
    }


    public List<VideoFile> FilesToConvert { get; set; } = new List<VideoFile>();

    public string FolderName { get; set; }


    public int TotalFiles => FilesToConvert.Count;

    public int ConvertedFiles
    {
        get
        {
            return FilesToConvert.Count(file => file.MainVideoConversionState == VideoFile.ConversionState.Converted);
        }
    }

    public int ProxiedFiles
    {
        get { return FilesToConvert.Count(file => file.ProxyConversionState == VideoFile.ConversionState.Converted); }
    }

    public bool HasProxyErrors
    {
        get { return FilesToConvert.Any(file => file.ProxyConversionState == VideoFile.ConversionState.Error); }
    }

    public bool HasConversionErrors
    {
        get { return FilesToConvert.Any(file => file.MainVideoConversionState == VideoFile.ConversionState.Error); }
    }

    private string GetProgressColor(VideoFile.ConversionState state)
    {
        return state switch
        {
            VideoFile.ConversionState.Converting => "LightGray",
            VideoFile.ConversionState.Converted => "LightGreen",
            VideoFile.ConversionState.Error => "Orange",
            VideoFile.ConversionState.Pending => "LightGray",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private string GetProgressIcon(VideoFile.ConversionState state)
    {
        return state switch
        {
            VideoFile.ConversionState.Converting => "\u231b\ufe0f",
            VideoFile.ConversionState.Converted => "\ud83d\udc4d\ufe0f",
            VideoFile.ConversionState.Error => "\ud83d\udc4e\ufe0f",
            VideoFile.ConversionState.Pending => "\u2753\ufe0f",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public string MainProgressColor => GetProgressColor(ConvertedState);

    public string ProxyProgressColor => GetProgressColor(ProxyState);

    public string MainProgressIcon => GetProgressIcon(ConvertedState);

    public string ProxyProgressIcon => GetProgressIcon(ProxyState);
}