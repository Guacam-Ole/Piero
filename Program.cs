using Avalonia;
using System;
using System.IO;

namespace Piero;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        CopyConfig();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static void CopyConfig()
    {
        if (File.Exists("config.json") || !File.Exists("config.default.json")) return;
        File.Copy("config.default.json","config.json");
        Console.WriteLine("Copied config");
    }
}