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