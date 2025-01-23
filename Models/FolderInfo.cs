using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Piero.Models;

public class FolderInfo:INotifyPropertyChanged
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
        get { return _mainConverting; }
        set { SetField(ref _mainConverting, value); }
    }

    public int MainPending
    {
        get { return _mainPending; }
        set { SetField(ref _mainPending, value); }
    }

    public int MainError
    {
        get { return _mainError; }
        set { SetField(ref _mainError, value); }
    }
    
    public int MainProgress
    {
        get { return _mainProgress; }
        set { SetField(ref _mainProgress, value); }
    }

    public int MainConverted
    {
        get { return _mainConverted; }
        set { SetField(ref _mainConverted, value); }
    }

    public int ProxyConverting
    {
        get { return _proxyConverting; }
        set { SetField(ref _proxyConverting, value); }
    }

    public int ProxyPending
    {
        get { return _proxyPending; }
        set { SetField(ref _proxyPending, value); }
    }

    public int ProxyError
    {
        get { return _proxyError; }
        set { SetField(ref _proxyError, value); }
    }
    
    public int ProxyProgress
    {
        get { return _proxyProgress; }
        set { SetField(ref _proxyProgress, value); }
    }

    public int ProxyConverted
    {
        get { return _proxyConverted; }
        set { SetField(ref _proxyConverted, value); }
    }


    private int _filesTotal;
    public int FilesTotal
    {
        get { return _filesTotal; }
        set { SetField(ref _filesTotal, value); }
    }
    
    public List<VideoFile> FilesToConvert { get; set; } = new List<VideoFile>();

    public string FolderName { get; set; }

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

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    
    
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}