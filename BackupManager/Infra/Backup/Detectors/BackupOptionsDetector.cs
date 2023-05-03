using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BackupManager.Domain.Configuration;
using BackupManager.Domain.Enums;
using BackupManager.Domain.Mapping;
using BackupManager.Domain.Settings;
using JsonSerialization;
using MediaDevices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Temporaries;

namespace BackupManager.Infra.Backup.Detectors;

public class BackupOptionsDetector
{
    private readonly IEnumerable<string>? mSubscribedDirectories;
    private readonly IJsonSerializer mJsonSerializer;
    private readonly ILogger<BackupOptionsDetector> mLogger;

    public BackupOptionsDetector(IOptions<BackupServiceConfiguration> configuration,
        IJsonSerializer objectSerializer,
        ILogger<BackupOptionsDetector> logger)
    {
        mSubscribedDirectories = configuration.Value.SubscribedDirectories;
        mJsonSerializer = objectSerializer ?? throw new ArgumentNullException(nameof(objectSerializer)); 
        mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public List<BackupSettings>? DetectBackupOptions()
    {
        mLogger.LogInformation($"Detecting backup settings...");

        List<BackupSettings>? settingsList = null;
        IEnumerable<BackupSettings>? settingsFilesFromDrives = TryGetSettingsFromDrives();
        IEnumerable<BackupSettings>? settingsFilesFromMediaDevices = TryGetSettingsFromMediaDevices();
        IEnumerable<BackupSettings>? settingsFilesFromSubscribedDirectories = TryGetSettingsFromSubscribedDirectories();
        
        if (settingsFilesFromDrives is not null)
        {
            settingsList ??= new List<BackupSettings>();
            settingsList.AddRange(settingsFilesFromDrives);
        }
        
        if (settingsFilesFromMediaDevices is not null)
        {
            settingsList ??= new List<BackupSettings>();
            settingsList.AddRange(settingsFilesFromMediaDevices);
        }
        
        if (settingsFilesFromSubscribedDirectories is not null)
        {
            settingsList ??= new List<BackupSettings>();
            settingsList.AddRange(settingsFilesFromSubscribedDirectories);
        }

        return settingsList;
    }

    // TODO DOR test real drive situation.
    private IEnumerable<BackupSettings>? TryGetSettingsFromDrives()
    {
        List<BackupSettings>? settingList = null;
        
        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType != DriveType.Removable)
            {
                continue;
            }
            
            mLogger.LogInformation($"Detected drive {drive.Name}");

            BackupSettings? settings = TryGetSettingsFileFromDirectory(drive.Name);

            if (settings is null)
            {
                continue;
            }
            
            settingList ??= new List<BackupSettings>();
            settingList.Add(settings);
        }

        return settingList;
    }

#pragma warning disable CA1416
    private IEnumerable<BackupSettings>? TryGetSettingsFromMediaDevices()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }
        
        List<BackupSettings>? settingList = null;
        
        foreach (MediaDevice device in MediaDevice.GetDevices())
        {
            try
            {
                device.Connect();
            
                mLogger.LogInformation($"Detected device {device.Description}");

                const string deviceStorageRootPath = @"\Internal shared storage";
                MediaDirectoryInfo? deviceRootDirectory = device.GetDirectoryInfo(deviceStorageRootPath);

                if (deviceRootDirectory is null)
                {
                    mLogger.LogError($"Could not find root path of device {device.Description}");
                    return null;
                }
                
                BackupSettings? settings = TryGetSettingsFileFromMediaDeviceDirectory(deviceRootDirectory, device.Description);

                if (settings is null)
                {
                    continue;
                }
            
                settingList ??= new List<BackupSettings>();
                settingList.Add(settings);
            
                device.Disconnect();
                device.Dispose();
            }
            catch (Exception ex)
            {
                mLogger.LogError(ex, $"Failed to get settings from media device'{device.Description}");
            }
        }

        return settingList;
    }
#pragma warning restore CA1416

    private IEnumerable<BackupSettings>? TryGetSettingsFromSubscribedDirectories()
    {
        if (mSubscribedDirectories is null)
        {
            mLogger.LogDebug("No subscribed directories");
            return null;
        }

        List<BackupSettings>? settingsList = null;
        foreach (string subscribedDirectory in mSubscribedDirectories)
        {
            BackupSettings? settings = TryGetSettingsFileFromDirectory(subscribedDirectory);
            if (settings is null)
            {
                continue;
            }

            settingsList ??= new List<BackupSettings>();
            settingsList.Add(settings);
        }

        return settingsList;
    }
    
#pragma warning disable CA1416
    private BackupSettings? TryGetSettingsFileFromMediaDeviceDirectory(MediaDirectoryInfo deviceRootDirectory, string mediaDeviceName)
    {
        MediaFileInfo[] backupSettingsFiles = deviceRootDirectory.EnumerateFiles($"*{Consts.SettingsFileName}").ToArray();

        if (backupSettingsFiles.Length == 0)
        {
            mLogger.LogInformation($"Could not find settings file in directory '{deviceRootDirectory.FullName}'");
            return null;
        }
        
        if (backupSettingsFiles.Length > 1)
        {
            mLogger.LogInformation($"Found more than one setting files in directory '{deviceRootDirectory.FullName}', taking only the first");
        }

        MediaFileInfo backupSettingsMediaFileInfo = backupSettingsFiles[0];
        using TempFile tempMediaDirectoryBackSettingsFilePath = new(Path.Combine(Consts.TempDirectoryPath, Path.GetRandomFileName()));
        mLogger.LogInformation($"Copying '{backupSettingsMediaFileInfo.FullName}' to {tempMediaDirectoryBackSettingsFilePath}");
        _ = Directory.CreateDirectory(Consts.TempDirectoryPath);
        
        backupSettingsMediaFileInfo.CopyTo(tempMediaDirectoryBackSettingsFilePath.Path);
        BackupSettings? settings = TryGetBackupSettingsFromFile(tempMediaDirectoryBackSettingsFilePath.Path,
            rootDirectory: deviceRootDirectory.FullName,
            SourceType.MediaDevice);

        if (settings is not null)
        {
            settings.SearchMethod = SearchMethod.FilePath;
            settings.MediaDeviceName = mediaDeviceName;
            settings.RootDirectory = deviceRootDirectory.Name;
        }
        
        return settings;
    }
#pragma warning restore CA1416

    private BackupSettings? TryGetSettingsFileFromDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            mLogger.LogInformation($"Subscribed directory '{directory}' does not exist");
            return null;
        }

        string[] backupSettingsFiles = Directory.GetFiles(directory, $"*{Consts.SettingsFileName}", SearchOption.AllDirectories);
        if (backupSettingsFiles.Length == 0)
        {
            mLogger.LogInformation($"Could not find settings file in directory '{directory}'");
            return null;
        }
        
        if (backupSettingsFiles.Length > 1)
        {
            mLogger.LogInformation($"Found more than one setting files in directory '{directory}', taking only the first");
        }

        string backupSettingsFilePath = backupSettingsFiles[0];
        BackupSettings? settings = TryGetBackupSettingsFromFile(backupSettingsFilePath, rootDirectory: directory, SourceType.DriveOrDirectory);
        
        if (settings is not null)
        {
            settings.SearchMethod = SearchMethod.Hash;
        }
        
        return settings;
    }
    
    private BackupSettings? TryGetBackupSettingsFromFile(string backupSettingsFilePath, string? rootDirectory, SourceType sourceType)
    {
        try
        {
            BackupSettings settings = mJsonSerializer.Deserialize<BackupSettings>(backupSettingsFilePath);
            updateBackupSettings(settings, rootDirectory, sourceType);
            return settings;
        }
        catch (Exception ex)
        {
            mLogger.LogError(ex, $"Failed to deserialize '{backupSettingsFilePath}'");
            return null;
        }
    }

    private void updateBackupSettings(BackupSettings backupSettings, string? rootDirectory, SourceType sourceType)
    {
        if (backupSettings.RootDirectory is not null && rootDirectory is not null)
        {
            backupSettings.RootDirectory = rootDirectory;
        }

        foreach (DirectoriesMap directorySourceToDirectoryDestination in backupSettings.DirectoriesSourcesToDirectoriesDestinationMap)
        {
            if (string.IsNullOrWhiteSpace(directorySourceToDirectoryDestination.DestRelativeDirectory))
            {
                directorySourceToDirectoryDestination.DestRelativeDirectory = Consts.BackupsDirectoryPath;
            }
        }

        backupSettings.SourceType = sourceType;
    }
}