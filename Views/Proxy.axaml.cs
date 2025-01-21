using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace Piero.Views;

public partial class Proxy : Window
{
    public EventHandler<FolderEventArgs> FolderAdded;

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

        var selectedFolder=folders.ToList().FirstOrDefault();
        if (selectedFolder != null)
        {
            FolderAdded?.Invoke(sender, new FolderEventArgs(selectedFolder.Path.AbsolutePath) { });
        }
    }
}