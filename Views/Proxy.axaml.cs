using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Piero.Models;

namespace Piero.Views;

public partial class Proxy : Window
{
    private readonly Config _config;
    public EventHandler<FolderEventArgs> FolderAdd;
    public EventHandler<FolderEventArgs> FolderRemove;
    public EventHandler<SelectionChangedEventArgs> SelectionChanged;

    public Proxy(Config config)
    {
        _config = config;
        InitializeComponent();
        this.Hide();
    }


    private async void AddFolder_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this) ?? throw new Exception("Error receiving root");
        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                AllowMultiple = false, Title = "Select Folder to watch for movies"
            }
        );

        var selectedFolder = folders.ToList().FirstOrDefault();
        if (selectedFolder != null)
        {
            FolderAdd?.Invoke(sender, new FolderEventArgs(selectedFolder.Path.AbsolutePath) { });
        }
    }

    private void RemoveFolder_Click(object? sender, RoutedEventArgs e)
    {
        var selectedItems = DisplayedFolders.SelectedItems.Cast<FolderInfo>().ToList();
        foreach (var item in selectedItems)
        {
            FolderRemove?.Invoke(sender, new FolderEventArgs(item.FolderName));
        }
    }

    private void Grid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        SelectionChanged?.Invoke(sender, e);
    }

    private async Task DisplayFolder(string? subfolder)
    {
        var selectedItem = DisplayedFolders.SelectedItems.Cast<FolderInfo>().FirstOrDefault();
        if (selectedItem==null) return;
        
        var path = subfolder==null? selectedItem.FolderName: Converter.GetPath(selectedItem.FolderName,subfolder);
        await GetTopLevel(this).Launcher.LaunchDirectoryInfoAsync(new DirectoryInfo(path));
    }
    
    private void DisplayOriginal_Click(object? sender, RoutedEventArgs e)
    {
        DisplayFolder(null).Wait();        
    }

    private void DisplayConverted_Click(object? sender, RoutedEventArgs e)
    {
        DisplayFolder(_config.VideoPath).Wait();
    }

    private void DisplayProxy_Click(object? sender, RoutedEventArgs e)
    {
        DisplayFolder(_config.ProxyPath).Wait();
    }
    
}