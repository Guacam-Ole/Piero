using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.Logging;
using Piero.Models;

namespace Piero.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ILogger<MainWindowViewModel> _logger;
    public ObservableCollection<FolderInfo> Folders { get; set; }
    public Config Config { get; set; }

    public MainWindowViewModel()
    {
    }

    public void RefreshData(List<FolderInfo> folderInfo)
    {
        Folders.Clear();
        foreach (var folder in folderInfo)
        {
            Folders.Add(folder);
        }
        //Folders = new ObservableCollection<FolderInfo>(folderInfo);
    }

    public void RefreshSingleItem(string folderName, VideoFile fileInfo)
    {
        var file = Folders.First(f => f.FolderName == folderName).FilesToConvert
            .First(f => f.FullName == fileInfo.FullName);
        file.ProxyConversionState = fileInfo.ProxyConversionState;
        file.ProxyProgress = fileInfo.ProxyProgress;
        file.MainVideoConversionState = fileInfo.MainVideoConversionState;
        file.MainProgress = fileInfo.MainProgress;
    }

    public MainWindowViewModel(ILogger<MainWindowViewModel> logger, Config config)
    {
        _logger = logger;
        Config = config;
        _logger.LogDebug("UI initialized");

        Folders = [];
    }
}