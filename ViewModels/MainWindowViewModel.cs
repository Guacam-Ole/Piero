using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Animation;
using Microsoft.Extensions.Logging;
using Piero.Models;

namespace Piero.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly Config _config;
    public ObservableCollection<FolderInfo> Folders { get; set; }
    public Config Config { get; set; }

    public MainWindowViewModel()
    {
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
                    FileName = $"dummy_{i}.txt", FileDate = DateTime.Now.AddDays(-2),
                    FilePath = "/etc/whatever/you/think/could/be/useful",
                    MainVideoConversionState = (VideoFile.ConversionState)(i % 4),
                    ProxyConversionState = VideoFile.ConversionState.Converted,
                });
            }

            dirs.Add(folder);
        }

        Folders = new ObservableCollection<FolderInfo>(dirs);
    }
}