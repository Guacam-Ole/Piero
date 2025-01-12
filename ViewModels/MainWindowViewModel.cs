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
    public ObservableCollection<VideoFile> FilesToConvert { get; set; }

    public MainWindowViewModel(ILogger<MainWindowViewModel> logger)
    {
        _logger = logger;

        var files = new List<VideoFile>();
        for (int i = 0; i < 100; i++)
        {
            files.Add(new VideoFile
            {
                FileName = $"dummy_{i}.txt", FileDate = DateTime.Now.AddDays(-2),
                FilePath = "/etc/whatever/you/think/could/be/useful",
                MainVideoConversionState = (VideoFile.ConversionState) (i%4),
                ProxyConversionState = VideoFile.ConversionState.Pending,
            });    
        }
        
        FilesToConvert = new ObservableCollection<VideoFile>(files);
    }
}