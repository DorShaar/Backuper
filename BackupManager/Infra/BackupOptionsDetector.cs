using System;
using System.Collections.Generic;
using System.IO;
using BackupManager.App.Serialization;
using BackupManager.Domain.Configuration;
using BackupManager.Domain.Mapping;
using BackupManager.Domain.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BackupManager.Infra;

public class BackupOptionsDetector
{
    private readonly IEnumerable<string>? mSubscribedDirectories;
    private readonly IObjectSerializer mObjectSerializer;
    private readonly ILogger<BackupOptionsDetector> mLogger;

    public BackupOptionsDetector(IOptions<BackupServiceConfiguration> configuration,
        IObjectSerializer objectSerializer,
        ILogger<BackupOptionsDetector> logger)
    {
        mSubscribedDirectories = configuration.Value.SubscribedDirectories;
        mObjectSerializer = objectSerializer ?? throw new ArgumentNullException(nameof(objectSerializer)); 
        mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public List<BackupSettings>? DetectBackupOptions()
    {
        mLogger.LogInformation($"Detecting backup settings...");
        
        List<BackupSettings>? settingsFiles = TryGetSettingsFromDrives();
        IEnumerable<BackupSettings>? settingsFilesFromSubscribedDirectories = TryGetSettingsFromSubscribedDirectories();
        
        if (settingsFilesFromSubscribedDirectories is not null)
        {
            settingsFiles ??= new List<BackupSettings>();
            settingsFiles.AddRange(settingsFilesFromSubscribedDirectories);
        }

        return settingsFiles;
    }

    // TODO DOR test real drive situation.
    private List<BackupSettings>? TryGetSettingsFromDrives()
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
        BackupSettings? settings = TryGetBackupSettingsFromFile(backupSettingsFilePath, rootDirectory: directory);
        
        return settings;
    }
    
    private BackupSettings? TryGetBackupSettingsFromFile(string backupSettingsFilePath, string? rootDirectory)
    {
        try
        {
            BackupSettings settings = mObjectSerializer.Deserialize<BackupSettings>(backupSettingsFilePath);
            updateBackupSettings(settings, rootDirectory);
            return settings;
        }
        catch (Exception ex)
        {
            mLogger.LogInformation($"Failed to deserialize '{backupSettingsFilePath}'", ex);
            return null;
        }
    }

    private void updateBackupSettings(BackupSettings backupSettings, string? rootDirectory)
    {
        if (backupSettings.RootDirectory is not null && rootDirectory is not null)
        {
            backupSettings.RootDirectory = rootDirectory;
        }

        foreach (DirectoriesMap directorySourceToDirectoryDestination in backupSettings.DirectoriesSourcesToDirectoriesDestinationMap)
        {
            if (string.IsNullOrWhiteSpace(directorySourceToDirectoryDestination.DestRelativeDirectory))
            {
                directorySourceToDirectoryDestination.DestRelativeDirectory = Consts.DataDirectoryPath;
            }
        }
    }
}