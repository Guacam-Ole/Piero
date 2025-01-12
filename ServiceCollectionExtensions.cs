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
            if (!ConfigurationValuesComplete(config.FfmpegPath, config.ProxyPath, config.StartupProxy,
                    config.StartupConversion, config.VideoPath))
                throw new ArgumentException("At least one of your configuration values is missing.");
            return config;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private static bool ConfigurationValuesComplete(params string[] valuesToCheck)
    {
        return valuesToCheck.All(value => !string.IsNullOrEmpty(value));
    }

    public static void AddServices(this IServiceCollection serviceCollection)
    {
        var config = ReadAndValidateConfiguration();
        serviceCollection.AddSingleton(config);
        serviceCollection.AddTransient<MainWindowViewModel>();
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