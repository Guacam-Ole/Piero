using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Piero.Models;

namespace Piero.Views;

public partial class Proxy : Window
{
    public EventHandler<FolderEventArgs> FolderAdd;
    public EventHandler<FolderEventArgs> FolderRemove;
    public EventHandler<FolderEventArgs> FolderOpen;
    public EventHandler<SelectionChangedEventArgs> SelectionChanged;

    public Proxy()
    {
        InitializeComponent();
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
}