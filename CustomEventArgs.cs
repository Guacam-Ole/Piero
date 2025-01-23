using System;

namespace Piero;

public class FolderEventArgs : EventArgs
{
    public FolderEventArgs(string folder)
    {
        Folder = folder;
    }

    public string Folder { get; set; }
}

public class FfmpegEventArgs : EventArgs
{
    public Converter.ConversionInfo? ConversionInfo { get; set; }
    public int Progress { get; set; }
    public bool IsFinished { get; set; }

    public FfmpegEventArgs(Converter.ConversionInfo conversionInfo, int progress)
    {
        ConversionInfo = conversionInfo;
        Progress = progress;
        IsFinished = progress == 100;
    }
}

public class WatcherEventArgs : EventArgs
{
    public enum Operations
    {
        Deleted,
        Created,
        Renamed
    }

    public string Filename { get; init; } = string.Empty;
    public string Directory { get; init; } = string.Empty;
    public Operations Operation { get; init; }
}