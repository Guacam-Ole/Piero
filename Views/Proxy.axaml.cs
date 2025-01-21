using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Piero.Models;

namespace Piero.Views;

public partial class Proxy : Window
{
    public EventHandler<FolderEventArgs> FolderAdded;
    public EventHandler ConfigSaveHandler;

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        ConfigSaveHandler?.Invoke(this, EventArgs.Empty);
    }

    public Proxy()
    {
        InitializeComponent();
    }

    private async void AddFolder_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        var folder = await topLevel.StorageProvider.OpenFolderPickerAsync( new FolderPickerOpenOptions
                {
                    AllowMultiple = false, Title = "Select Folder" 
                }
        
        );
        
        if (folder.Any())
        {
            FolderAdded?.Invoke(sender, new FolderEventArgs(folder.First().Path.AbsolutePath) { });
        }
    }


}