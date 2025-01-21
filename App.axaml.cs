using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Piero.Models;
using Piero.ViewModels;
using Piero.Views;

namespace Piero;

public partial class App : Application
{
    private Watcher? _watcher;
    private Config? _config;
    private Converter _converter;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddServices();

        var services = serviceCollection.BuildServiceProvider();
        var mainViewModel = services.GetRequiredService<MainWindowViewModel>();
        _watcher = services.GetRequiredService<Watcher>();
        _config = services.GetRequiredService<Config>();
        _converter=services.GetRequiredService<Converter>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            var proxy = new Proxy
            {
                DataContext = mainViewModel
            };
            proxy.FolderAdded += OnFolderAdded;
            proxy.Closed += OnProxyClosed;
            desktop.MainWindow = proxy;
        }


        base.OnFrameworkInitializationCompleted();
    }

    private void OnProxyClosed(object? sender, EventArgs e)
    {
        var jsonConf = JsonConvert.SerializeObject(_config);
        File.WriteAllText("config.json", jsonConf);
    }

    private async void OnFolderAdded(object? sender, FolderEventArgs e)
    {
        var conversionTasks=new List<Task>();
        if (!_config!.AddPath(e.Folder)) return; // already exists
        _watcher!.AddWatcher(e.Folder);
        foreach (var file in new DirectoryInfo(e.Folder).GetFiles())
        {
            if (file.Extension==".mp4")
            {
               conversionTasks.Add(_converter!.StartConversion(e.Folder, _config.VideoPath, file.FullName,
                    _config.FfmpegConfigs[_config.ConversionIndex].Command));
            }
        }
        await Task.WhenAll(conversionTasks);
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