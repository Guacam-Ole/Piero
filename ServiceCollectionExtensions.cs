using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NReco.Logging.File;
using Piero.Models;
using Piero.ViewModels;

namespace Piero;

public static class ServiceCollectionExtensions
{
    private static Config ReadAndValidateConfiguration()
    {
        try
        {
            if (!File.Exists(@"config.json")) throw new FileNotFoundException(@"config.json");
            var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(@"config.json"));
            if (config == null) throw new JsonException("Cannot read config. Something wrong in the format?");
            return config;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public static void AddServices(this IServiceCollection serviceCollection)
    {
        var config = ReadAndValidateConfiguration();
        serviceCollection.AddSingleton(config);
        serviceCollection.AddSingleton<Watcher>();
        serviceCollection.AddTransient<MainWindowViewModel>();
        serviceCollection.AddScoped<Converter>();
        serviceCollection.AddScoped<Captions>();
        serviceCollection.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddSimpleConsole(options =>
                {
                    options.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
                });
                var logFile = config.LogFile; 
                logging.AddFile(logFile, conf =>
                {
                    conf.MinLevel = LogLevel.Debug;
                    conf.Append = true;
                    conf.MaxRollingFiles = 1;
                    conf.FileSizeLimitBytes = 100000;
                });
            }
        );
    }
}