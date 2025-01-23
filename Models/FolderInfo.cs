using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Piero.Models;

public sealed class FolderInfo:INotifyPropertyChanged
{
    private int _mainConverted;
    private int _mainConverting;
    private int _mainPending;
    private int _mainError;
    private int _mainProgress;

    private int _proxyConverted;
    private int _proxyConverting;
    private int _proxyPending;
    private int _proxyError;
    private int _proxyProgress;

    
    public int MainConverting
    {
        get => _mainConverting;
        set => SetField(ref _mainConverting, value);
    }

    public int MainPending
    {
        get => _mainPending;
        set => SetField(ref _mainPending, value);
    }

    public int MainError
    {
        get => _mainError;
        set => SetField(ref _mainError, value);
    }
    
    public int MainProgress
    {
        get => _mainProgress;
        set => SetField(ref _mainProgress, value);
    }

    public int MainConverted
    {
        get => _mainConverted;
        set => SetField(ref _mainConverted, value);
    }

    public int ProxyConverting
    {
        get => _proxyConverting;
        set => SetField(ref _proxyConverting, value);
    }

    public int ProxyPending
    {
        get => _proxyPending;
        set => SetField(ref _proxyPending, value);
    }

    public int ProxyError
    {
        get => _proxyError;
        set => SetField(ref _proxyError, value);
    }
    
    public int ProxyProgress
    {
        get => _proxyProgress;
        set => SetField(ref _proxyProgress, value);
    }

    public int ProxyConverted
    {
        get => _proxyConverted;
        set => SetField(ref _proxyConverted, value);
    }


    private int _filesTotal;
    public int FilesTotal
    {
        get => _filesTotal;
        set => SetField(ref _filesTotal, value);
    }
    
    public List<VideoFile> FilesToConvert { get; set; } = [];

    public required string FolderName { get; set; }

    public void RecalculateMain(int? progress)
    {
        if (progress != null) MainProgress = progress.Value;
        MainPending=FilesToConvert.Count(f => f.MainVideoConversionState == VideoFile.ConversionState.Pending);
        MainConverting=FilesToConvert.Count(f => f.MainVideoConversionState == VideoFile.ConversionState.Converting);
        MainConverted=FilesToConvert.Count(f => f.MainVideoConversionState == VideoFile.ConversionState.Converted);
        MainError=FilesToConvert.Count(f => f.MainVideoConversionState == VideoFile.ConversionState.Error);
        FilesTotal = FilesToConvert.Count;
    }
    
    public void RecalculateProxy(int? progress)
    {
        if (progress != null) ProxyProgress = progress.Value;
        ProxyPending=FilesToConvert.Count(f => f.ProxyConversionState == VideoFile.ConversionState.Pending);
        ProxyConverting=FilesToConvert.Count(f => f.ProxyConversionState == VideoFile.ConversionState.Converting);
        ProxyConverted=FilesToConvert.Count(f => f.ProxyConversionState == VideoFile.ConversionState.Converted);
        ProxyError=FilesToConvert.Count(f => f.ProxyConversionState == VideoFile.ConversionState.Error);
        FilesTotal = FilesToConvert.Count;
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        OnPropertyChanged(propertyName);
    }
}