using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
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
    private ILogger<App> _logger;

    private Watcher _watcher;
    private Config _config;
    private Converter _converter;
    private Captions _captions;
    private MainWindowViewModel _mainViewModel;
    private readonly List<FolderInfo> _queue = [];
    private bool _queueIsProcessing;


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
        _watcher.FileChanged += WatcherFileChanged;
        _config = services.GetRequiredService<Config>();
        _converter = services.GetRequiredService<Converter>();
        _captions = services.GetRequiredService<Captions>();
        _converter.ProgressChanged += ProgressChanged;
        _logger = services.GetRequiredService<ILogger<App>>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();
            var proxy = new Proxy(_config)
            {
                DataContext = _mainViewModel
            };
            proxy.FolderAdd += OnFolderAdd;
            proxy.FolderRemove += OnFolderRemove;
            proxy.SelectionChanged += OnSelectionChanged;
            proxy.Closed += OnProxyClosed;
            desktop.MainWindow = proxy;
        }

        base.OnFrameworkInitializationCompleted();
        foreach (var path in _config.Paths)
        {
            AddFolderToQueue(path, true);
            _watcher!.AddWatcher(path);
        }

        _watcher.ResetAllWatchers(_config.Paths);
        ProcessQueue();
    }
    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var datagrid = (DataGrid)sender!;
        _mainViewModel.ItemsSelected = datagrid.SelectedItems.Count > 0;
        _mainViewModel.SingleItemSelected = datagrid.SelectedItems.Count == 1;
    }

    private void OnFolderRemove(object? sender, FolderEventArgs e)
    {
        _config.Paths.Remove(e.Folder);
        _queue.RemoveAll(q => q.FolderName == e.Folder);
        UpdateViewModel();
    }

    private async void WatcherFileChanged(object? sender, WatcherEventArgs e)
    {
        if (e.Operation != WatcherEventArgs.Operations.Created)
        {
            _logger.LogDebug("Ignoring watcher operation '{operation}'", e.Operation);
            return;
        }

        var folderConf = _queue.First(f => f.FolderName == e.Directory);
        AddSingleFileToQueue(new FileInfo(e.Filename), folderConf);
        await ProcessQueue();
    }

    private void ProgressChanged(object? sender, FfmpegEventArgs e)
    {
        if (e.ConversionInfo == null) return;

        var matchingFolder = _queue.First(q => q.FolderName == e.ConversionInfo.FolderName);
        var matchingFile = matchingFolder.FilesToConvert.First(f => f.FullName == e.ConversionInfo.VideoFile.FullName);

        var newState = VideoFile.ConversionState.Converting;
        if (e.IsFinished) newState = VideoFile.ConversionState.Converted;
        if (e.IsError) newState = VideoFile.ConversionState.Error;

        if (e.ConversionInfo.IsMainConversion)
        {
            if (matchingFile.MainVideoConversionState != VideoFile.ConversionState.Error)
                matchingFile.MainVideoConversionState = newState;
        }
        else
        {
            if (matchingFile.ProxyConversionState != VideoFile.ConversionState.Error)
                matchingFile.ProxyConversionState = newState;
        }

        if (e.ConversionInfo.IsMainConversion)
        {
            matchingFile.MainProgress = e.Progress;
        }
        else
        {
            matchingFile.ProxyProgress = e.Progress;
        }

        UpdateFileInfo(matchingFolder, matchingFile.MainProgress, matchingFile.ProxyProgress);
    }

    private void OnProxyClosed(object? sender, EventArgs e)
    {
        var jsonConf = JsonConvert.SerializeObject(_config);
        File.WriteAllText("config.json", jsonConf);
    }

    private void AddFolderToQueue(string folder, bool init = false)
    {
        if (!init && !_config!.AddPath(folder))
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
            AddSingleFileToQueue(file, folderConf);
        }

        UpdateViewModel();
    }

    private void AddSingleFileToQueue(FileInfo file, FolderInfo folderConf)
    {
        if (!_config!.Extensions.Contains(file.Extension) ||
            folderConf.FilesToConvert.Any(f => f.FullName == file.FullName)) return;

        folderConf.FilesToConvert.Add(new VideoFile
        {
            FullName = file.FullName,
            HumanReadableFileSize = HumanReadableFileSize(file.Length),
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
    }

    private static string HumanReadableFileSize(long sizeInBytes)
    {
        double size = sizeInBytes;

        string[] suffixes = ["Bytes", "KBytes", "MBytes", "GBytes"];
        var suffix = suffixes[0];
        for (var i = 1; i < suffixes.Length; i++)
        {
            if (size < 1024) break;
            size = size / 1024;
            suffix = suffixes[i];
        }

        return $"{size:0.00} {suffix}";
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
            _queueIsProcessing = true;
            _logger.LogDebug("Start processing Queue");
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

        _queueIsProcessing = false;
        _logger.LogDebug("Finished processing Queue");
    }

    private async Task ConvertFolder(FolderInfo folder, bool mainVideo)
    {
        var conversion = _config.FfmpegConfigs[mainVideo ? _config.ConversionIndex : _config.ProxyIndex];
        if (string.IsNullOrWhiteSpace(conversion.Command)) return; // TODO: MessageBox
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
            var task = _converter!.StartConversion(folder.FolderName, file, conversion, mainVideo);
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

            UpdateFileInfo(folder, null, null);
            _captions.Title =
                $"Creating {(mainVideo ? "Main" : "Proxy")} Video for '{file.FullName}' [{file.HumanReadableFileSize}]";

            if (!_config.FfMpegParallelConversion)
            {
                await task;
            }
            else
            {
                conversionTasks.Add(task);
            }

            _captions.SetIdle();
        }

        if (_config.FfMpegParallelConversion)
        {
            await Task.WhenAll(conversionTasks);
        }
    }

    private void UpdateFileInfo(FolderInfo folder, int? mainProgress, int? proxyProgress)
    {
        _mainViewModel.RefreshSingleItem(folder, mainProgress, proxyProgress);
    }

    private async void OnFolderAdd(object? sender, FolderEventArgs e)
    {
        await ProcessFolder(e.Folder);
    }

    private async Task ProcessFolder(string folderName)
    {
        AddFolderToQueue(folderName);
        await ProcessQueue();
        _watcher!.AddWatcher(folderName);
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