using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Piero.Models;

public partial class FolderInfo : ObservableObject
{
    [ObservableProperty] private int _mainConverted;
    [ObservableProperty] private int _mainConverting;
    [ObservableProperty] private int _mainPending;
    [ObservableProperty] private int _mainError;
    [ObservableProperty] private int _mainProgress;

    [ObservableProperty] private int _proxyConverted;
    [ObservableProperty] private int _proxyConverting;
    [ObservableProperty] private int _proxyPending;
    [ObservableProperty] private int _proxyError;
    [ObservableProperty] private int _proxyProgress;

    [ObservableProperty] private int _filesTotal;


    public List<VideoFile> FilesToConvert { get; set; } = [];

    public required string FolderName { get; set; }

    public void RecalculateMain(int? progress)
    {
        if (progress != null) MainProgress = progress.Value;
        MainPending = FilesToConvert.Count(f => f.MainVideoConversionState == VideoFile.ConversionState.Pending);
        MainConverting = FilesToConvert.Count(f => f.MainVideoConversionState == VideoFile.ConversionState.Converting);
        MainConverted = FilesToConvert.Count(f => f.MainVideoConversionState == VideoFile.ConversionState.Converted);
        MainError = FilesToConvert.Count(f => f.MainVideoConversionState == VideoFile.ConversionState.Error);
        FilesTotal = FilesToConvert.Count;
    }

    public void RecalculateProxy(int? progress)
    {
        if (progress != null) ProxyProgress = progress.Value;
        ProxyPending = FilesToConvert.Count(f => f.ProxyConversionState == VideoFile.ConversionState.Pending);
        ProxyConverting = FilesToConvert.Count(f => f.ProxyConversionState == VideoFile.ConversionState.Converting);
        ProxyConverted = FilesToConvert.Count(f => f.ProxyConversionState == VideoFile.ConversionState.Converted);
        ProxyError = FilesToConvert.Count(f => f.ProxyConversionState == VideoFile.ConversionState.Error);
        FilesTotal = FilesToConvert.Count;
    }
}