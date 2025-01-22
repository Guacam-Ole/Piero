using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Piero.Models;
using Piero.ViewModels;
using Piero.Views;

namespace Piero;

public partial class App : Application
{
    private Watcher? _watcher;
    private Config _config;
    private Converter _converter;
    private MainWindowViewModel _mainViewModel;
    private ILogger<App> _logger;
    private List<FolderInfo> _queue;
    private bool _queueIsProcessing = false;


    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddServices();

        var services = serviceCollection.BuildServiceProvider();
        _mainViewModel = services.GetRequiredService<MainWindowViewModel>();
        _watcher = services.GetRequiredService<Watcher>();
        _config = services.GetRequiredService<Config>();
        _converter = services.GetRequiredService<Converter>();
        _converter.ProgressChanged += ProcessChanged;
        _logger = services.GetRequiredService<ILogger<App>>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            var proxy = new Proxy
            {
                DataContext = _mainViewModel
            };
            proxy.FolderAdded += OnFolderAdded;
            proxy.Closed += OnProxyClosed;
            desktop.MainWindow = proxy;
        }


        base.OnFrameworkInitializationCompleted();
    }

    private void ProcessChanged(object? sender, FfmpegEventArgs e)
    {
        var matchingFolder = _queue.First(q => q.FolderName == e.ConversionInfo.FolderName);
        var matchingFile = matchingFolder.FilesToConvert.First(f => f.FullName == e.ConversionInfo.VideoFile.FullName);
        if (e.IsFinished)
        {
            if (e.ConversionInfo.IsMainConversion)
            {
                matchingFile.MainVideoConversionState = VideoFile.ConversionState.Converted;
            }
            else
            {
                matchingFile.ProxyConversionState = VideoFile.ConversionState.Converted;
            }
        }

        if (e.ConversionInfo.IsMainConversion)
        {
            matchingFile.MainProgress = e.Progress;
        }
        else
        {
            matchingFile.ProxyProgress = e.Progress;
        }

        UpdateFileInfo(matchingFolder.FolderName, matchingFile);
    }

    private void OnProxyClosed(object? sender, EventArgs e)
    {
        var jsonConf = JsonConvert.SerializeObject(_config);
        File.WriteAllText("config.json", jsonConf);
    }

    private void AddFolderToQueue(string folder)
    {
        if (!_config!.AddPath(folder))
        {
            // TODO: Show MessageBox
            _logger.LogInformation("'{folder}' has already been added", folder);
            return;
        }

        var folderConf = _queue.FirstOrDefault(f => f.FolderName == folder);
        if (folderConf == null)
        {
            folderConf = new FolderInfo
            {
                FolderName = folder,
                FilesToConvert = []
            };
            _queue.Add(folderConf);
        }

        foreach (var file in new DirectoryInfo(folder).GetFiles())
        {
            if (!_config.Extensions.Contains(file.Extension) ||
                folderConf.FilesToConvert.Any(f => f.FullName == file.FullName)) continue;

            folderConf.FilesToConvert.Add(new VideoFile
            {
                FullName = file.FullName,
                ProxyConversionState =
                    Converter.TargetExists(file.FullName, _config.ProxyPath)
                        ? VideoFile.ConversionState.Converted
                        : VideoFile.ConversionState.Pending,
                MainVideoConversionState =
                    Converter.TargetExists(file.FullName, _config.VideoPath)
                        ? VideoFile.ConversionState.Converted
                        : VideoFile.ConversionState.Pending
            });

            _logger.LogDebug("Added '{file}' to queue", file.FullName);
            UpdateViewModel();
        }
    }

    private void UpdateViewModel()
    {
        _mainViewModel.RefreshData(_queue);
    }

    private async Task ProcessQueue()
    {
        if (_queueIsProcessing)
        {
            _logger.LogDebug("Queue is already being processed");
            return;
        }

        var queueHasEntries = true;
        while (queueHasEntries)
        {
            _logger.LogDebug("Start processing Queue");
            _queueIsProcessing = true;
            var pendingMainConversionFolders = _queue.Where(q =>
                q.FilesToConvert.Any(f => f.MainVideoConversionState == VideoFile.ConversionState.Pending)).ToList();
            var pendingProxyConversionFolders = _queue.Where(q =>
                q.FilesToConvert.Any(f => f.ProxyConversionState == VideoFile.ConversionState.Pending)).ToList();

            var mainFilesToConvert = pendingMainConversionFolders.SelectMany(q =>
                q.FilesToConvert.Where(f => f.MainVideoConversionState == VideoFile.ConversionState.Pending)).ToList();
            var proxyFilesToConvert = pendingProxyConversionFolders.SelectMany(q =>
                q.FilesToConvert.Where(f => f.ProxyConversionState == VideoFile.ConversionState.Pending)).ToList();
            if (mainFilesToConvert.Count == 0 && proxyFilesToConvert.Count == 0) queueHasEntries = false;

            _logger.LogDebug("Converting {count} main Video Files", mainFilesToConvert.Count);

            foreach (var folder in pendingMainConversionFolders)
            {
                await ConvertFolder(folder, true);
            }

            _logger.LogDebug("Converting {count} Proxy Files", proxyFilesToConvert.Count);
            foreach (var folder in pendingProxyConversionFolders)
            {
                await ConvertFolder(folder, false);
            }
        }
        
        _logger.LogDebug("Finished processing Queue");
    }

    private async Task ConvertFolder(FolderInfo folder, bool mainVideo)
    {
        var filesToConvert =
            (mainVideo
                ? folder.FilesToConvert.Where(q => q.MainVideoConversionState == VideoFile.ConversionState.Pending)
                : folder.FilesToConvert.Where(q => q.ProxyConversionState == VideoFile.ConversionState.Pending)
            ).ToList();

        _logger.LogDebug("Converting {Count} {type} files in '{Folder}", filesToConvert.Count,
            mainVideo ? "main" : "proxy", folder.FolderName);

        List<Task<bool>> conversionTasks = [];

        foreach (var file in filesToConvert)
        {
            var task = _converter!.StartConversion(folder.FolderName, file,
                _config.FfmpegConfigs[mainVideo ? _config.ConversionIndex : _config.ProxyIndex].Command, mainVideo);
            if (mainVideo)
            {
                file.MainVideoConversionState = VideoFile.ConversionState.Converting;
                file.MainProgress = 0;
            }
            else
            {
                file.ProxyConversionState = VideoFile.ConversionState.Converting;
                file.ProxyProgress = 0;
            }
            UpdateFileInfo(folder.FolderName, file);

            _mainViewModel.RefreshSingleItem(folder.FolderName, file);
            
            if (!_config.FfMpegParallelConversion)
            {
                await task;
            }
            else
            {
                conversionTasks.Add(task);
            }
            
        }

        if (_config.FfMpegParallelConversion)
        {
            await Task.WhenAll(conversionTasks);
        }
    }

    private void UpdateFileInfo(string folderName, VideoFile file)
    {
        _mainViewModel.RefreshSingleItem(folderName, file);
    }

    private async void OnFolderAdded(object? sender, FolderEventArgs e)
    {
        AddFolderToQueue(e.Folder);
        await ProcessQueue();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}