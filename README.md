# Piero
## About
Piero is a conversion and proxy tool for DaVinci. It works on most platforms but was especially designed with Linux in mind

## Installation
Select the package for your Operating System from the released. Install the correct .NET Runtime and start the application with `dotnet ./Piero.dll`


## Configuration
All Configuration can be done in the `config.json` - File. 

### Paths

>FfmpegPath
>>Location of `ffmpeg`

>ProxyPath
>>Where to save Proxy Files. _Can be relative (but not recommend)_

>VideoPath
>>Where to save main Conversions _Can be relative_

>Paths
>>Which directories to monitor for new files (can also be added in the UI) 

### Video Configuration
>Extensions
>>List of extensions which are considered video files and should be converted

>FfmpegConfigs
>>Conversionsettings

>ConversionIndex
>>Which conversion setting to use for main conversion (can be changed in the UI)

>ProxyIndex
>>Which conversion setting to use for proxy conversion (can be changed in the UI)

>FfMpegParallelConversion
>> Convert in Parallel instead of sequential conversion (not recommend)

>FfmpegPrefix
>> Change the default prefix for ffmpeg (not recommend)

### Additional Settings
>ShowHeader, ShowFooter
>> Show Header and/or Footer
 
>Logfile
>> Filename for logfile

