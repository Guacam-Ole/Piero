using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Piero;

public class Watcher
{
    public EventHandler<WatcherEventArgs> FileChanged;
    private readonly object _watcherLock = new();
    private readonly ILogger<Watcher> _logger;
    private List<FileSystemWatcher> _watchers = [];

    public Watcher(ILogger<Watcher> logger)
    {
        _logger = logger;
    }

    public void ResetAllWatchers(List<string> pathsToWatch)
    {
        lock (_watcherLock)
        {
            _watchers = [];
        }

        foreach (var path in pathsToWatch)
        {
            AddWatcher(path);
        }
    }

    public void AddWatcher(string pathToWatch)
    {
        var watcher = new FileSystemWatcher(pathToWatch);


        watcher.NotifyFilter = NotifyFilters.CreationTime
                               | NotifyFilters.DirectoryName
                               | NotifyFilters.FileName
                               | NotifyFilters.LastAccess
                               | NotifyFilters.LastWrite
                               | NotifyFilters.Size;

        watcher.Created += OnCreated;
        watcher.Renamed += OnRenamed;
        watcher.Deleted += OnDeleted;
        watcher.EnableRaisingEvents = true;

        lock (_watcherLock)
        {
            _watchers.Add(watcher);
        }

        _logger.LogDebug("Now watching '{path}' for changes", pathToWatch);
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        _logger.LogDebug("Renamed '{old}' to '{new}'", e.OldFullPath, e.FullPath);
        FileChanged?.Invoke(sender, new WatcherEventArgs
        {
            Operation = WatcherEventArgs.Operations.Renamed,
            Directory = ((FileSystemWatcher)sender).Path,
            Filename = e.FullPath
        });
    }

    private void OnDeleted(object sender, FileSystemEventArgs e)
    {
        FileChanged?.Invoke(sender, new WatcherEventArgs
        {
            Operation = WatcherEventArgs.Operations.Deleted,
            Directory = ((FileSystemWatcher)sender).Path,
            Filename = e.FullPath
        });
    }


    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        FileChanged?.Invoke(sender, new WatcherEventArgs
        {
            Operation = WatcherEventArgs.Operations.Created,
            Directory = ((FileSystemWatcher)sender).Path,
            Filename = e.FullPath
        });
        _logger.LogDebug("Created '{file}'", e.FullPath);
    }
}