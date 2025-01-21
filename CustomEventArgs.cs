using System;

namespace Piero;

public class FolderEventArgs:EventArgs
{
    public FolderEventArgs(string folder)
    {
        Folder = folder;
    }
    public string Folder { get; set; }
}

public class FfmpegEventArgs:EventArgs
{
    public FfmpegEventArgs(int id, TimeSpan duration, int percentage)
    {
        Id = id;
        Duration = duration;
        Percentage = percentage;
    }
    public int Id { get; set; }
    public TimeSpan Duration { get; set; }
    public int Percentage { get; set; }
}