using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Piero.Models;

namespace Piero.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ILogger<MainWindowViewModel> _logger;
    private ObservableCollection<FolderInfo> Folders { get; set; }
    public Config Config { get; set; }

    public MainWindowViewModel()
    {
    }

    public void RefreshData(List<FolderInfo> folderInfo)
    {
        Folders = new ObservableCollection<FolderInfo>(folderInfo);
    }

    public MainWindowViewModel(ILogger<MainWindowViewModel> logger, Config config)
    {
        _logger = logger;
        Config = config;

        var dirs = new List<FolderInfo>();

        for (int u = 0; u < 10; u++)
        {
            var folder = new FolderInfo
            {
                FolderName = "/etc/whatever/you/say",
                FilesToConvert = []
            };
            for (int i = 0; i < 100; i++)
            {
                folder.FilesToConvert.Add(new VideoFile
                {
                    FullName = $"/etc/whatever/you/think/could/be/useful/dummy_{i}.txt",
                    MainVideoConversionState = (VideoFile.ConversionState)(u % 4),
                    ProxyConversionState = VideoFile.ConversionState.Converted,
                });
            }

            dirs.Add(folder);
        }
        _logger.LogDebug("UI initialized");

        Folders = new ObservableCollection<FolderInfo>(dirs);
    }
}