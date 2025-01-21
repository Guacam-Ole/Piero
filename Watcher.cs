using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Piero;

public class Watcher
{
    private object _watcherLock = new object();
    private readonly ILogger<Watcher> _logger;
    private List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();

    public Watcher(ILogger<Watcher> logger)
    {
        _logger = logger;
    }

    public void ResetAllWatchers(List<string> pathsToWatch)
    {
        lock (_watcherLock)
        {
            watchers = [];
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
            watchers.Add(watcher);
        }
        _logger.LogDebug("Now watching '{path}' for changes", pathToWatch);
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        // TODO: Implement
        _logger.LogDebug("Renamed '{old}' to '{new}'", e.OldFullPath, e.FullPath);
    //    _logger.LogWarning("Renaming is not implemented yet");
    }

    private void OnDeleted(object sender, FileSystemEventArgs e)
    {
        _logger.LogDebug("Deleted '{file}''", e.FullPath);
  //      _logger.LogWarning("Deletion is not implemented yet");
    }



    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        _logger.LogDebug("Created '{file}'", e.FullPath);
        //_logger.LogWarning("Changes not implemented yet");
    }
}