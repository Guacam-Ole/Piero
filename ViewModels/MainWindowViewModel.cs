using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Piero.Models;

namespace Piero.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ILogger<MainWindowViewModel> _logger;
    public ObservableCollection<FolderInfo> Folders { get; set; }
    public Config Config { get; set; }
    public Captions Captions { get; set; }

    // Needed for UI-preview
    public MainWindowViewModel()
    {
    }

    public void RefreshData(List<FolderInfo> folderInfo)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Folders.Clear();
            foreach (var folder in folderInfo)
            {
                Folders.Add(folder);
            }
        });
    }

    public void RefreshSingleItem(FolderInfo folderInfo, int? mainProgress, int? proxyProgress)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var folder = Folders.First(f => f.FolderName == folderInfo.FolderName);
            folder.FilesToConvert = folderInfo.FilesToConvert;
            folder.RecalculateMain(mainProgress);
            folder.RecalculateProxy(proxyProgress);
        });
    }


    public MainWindowViewModel(ILogger<MainWindowViewModel> logger, Config config, Captions captions)
    {
        _logger = logger;
        Config = config;
        Captions = captions;
        _logger.LogDebug("UI initialized");

        Folders = []; 
        // new ObservableCollection<FolderInfo>
        // {
        //     new FolderInfo
        //     {
        //         FilesToConvert =
        //         [
        //             new VideoFile
        //             {
        //                 FullName = "ji",
        //                 MainProgress = 34,
        //                 MainVideoConversionState = VideoFile.ConversionState.Converting,
        //                 ProxyConversionState = VideoFile.ConversionState.Error,
        //                 ProxyProgress = 3
        //             }
        //         ],
        //         FolderName = "/etc", 
        //     }
        // };
    }
}