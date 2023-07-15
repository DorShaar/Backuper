using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.App.Backup.Detectors;
using BackupManager.Domain.Configuration;
using BackupManager.Domain.Enums;
using BackupManager.Domain.Settings;
using BackupManagerCore;
using BackupManagerCore.Settings;
using JsonSerialization;
using MediaDevices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Temporaries;

namespace BackupManager.Infra.Backup.Detectors;

public class BackupSettingsDetector : IBackupSettingsDetector
{
    private readonly IEnumerable<string>? mSubscribedDirectories;
    private readonly HashSet<string>? mSubscribedDrives;
    private readonly IJsonSerializer mJsonSerializer;
    private readonly ILogger<BackupSettingsDetector> mLogger;

    public BackupSettingsDetector(IOptions<BackupServiceConfiguration> configuration,
        IJsonSerializer objectSerializer,
        ILogger<BackupSettingsDetector> logger)
    {
        mSubscribedDirectories = configuration.Value.SubscribedDirectories;
        mSubscribedDrives = configuration.Value.SubscribedDrives;
        mJsonSerializer = objectSerializer ?? throw new ArgumentNullException(nameof(objectSerializer)); 
        mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<List<BackupSettings>?> DetectBackupSettings(CancellationToken cancellationToken)
    {
        mLogger.LogDebug($"Detecting backup settings...");

        List<BackupSettings>? settingsList = null;
        IEnumerable<BackupSettings>? settingsFilesFromDrives = await TryGetSettingsFromDrives(cancellationToken).ConfigureAwait(false);
        IEnumerable<BackupSettings>? settingsFilesFromMediaDevices = await TryGetSettingsFromMediaDevices(cancellationToken).ConfigureAwait(false);
        IEnumerable<BackupSettings>? settingsFilesFromSubscribedDirectories = await TryGetSettingsFromSubscribedDirectories(cancellationToken).ConfigureAwait(false);
        
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

    private async Task<IEnumerable<BackupSettings>?> TryGetSettingsFromDrives(CancellationToken cancellationToken)
    {
        List<BackupSettings>? settingList = null;
        if (mSubscribedDrives is null)
        {
            return settingList;
        }
        
        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            if (!mSubscribedDrives.Contains(drive.VolumeLabel))
            {
                continue;
            }
            
            mLogger.LogInformation($"Detected drive {drive.Name} '{drive.VolumeLabel}'");

            BackupSettings? settings = await TryGetSettingsFileFromDirectory(drive.Name, cancellationToken).ConfigureAwait(false);

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
    private async Task<IEnumerable<BackupSettings>?> TryGetSettingsFromMediaDevices(CancellationToken cancellationToken)
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

                string deviceStorageRootPath = @"\Internal shared storage";
                if (device.FriendlyName.Contains("Galaxy S23", StringComparison.OrdinalIgnoreCase))
                {
                    deviceStorageRootPath = "Internal storage";
                }
                
                MediaDirectoryInfo? deviceRootDirectory = device.GetDirectoryInfo(deviceStorageRootPath);

                if (deviceRootDirectory is null)
                {
                    mLogger.LogError($"Could not find root path of device {device.Description}");
                    return null;
                }
                
                BackupSettings? settings = await TryGetSettingsFileFromMediaDeviceDirectory(deviceRootDirectory,
                    device.Description,
                    cancellationToken).ConfigureAwait(false);

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
                mLogger.LogError(ex, $"Failed to get settings from media device '{device.Description}'");
            }
        }

        return settingList;
    }
#pragma warning restore CA1416

    private async Task<IEnumerable<BackupSettings>?> TryGetSettingsFromSubscribedDirectories(CancellationToken cancellationToken)
    {
        if (mSubscribedDirectories is null)
        {
            mLogger.LogDebug("No subscribed directories");
            return null;
        }

        List<BackupSettings>? settingsList = null;
        foreach (string subscribedDirectory in mSubscribedDirectories)
        {
            BackupSettings? settings = await TryGetSettingsFileFromDirectory(subscribedDirectory, cancellationToken).ConfigureAwait(false);
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
    private async Task<BackupSettings?> TryGetSettingsFileFromMediaDeviceDirectory(MediaDirectoryInfo deviceRootDirectory,
        string mediaDeviceName,
        CancellationToken cancellationToken)
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
        BackupSettings? settings = await TryGetBackupSettingsFromFile(tempMediaDirectoryBackSettingsFilePath.Path,
            rootDirectory: deviceRootDirectory.Name,
            SourceType.MediaDevice,
            cancellationToken).ConfigureAwait(false);

        if (settings is not null)
        {
            settings.MediaDeviceName = mediaDeviceName;
        }
        
        return settings;
    }
#pragma warning restore CA1416

    private async Task<BackupSettings?> TryGetSettingsFileFromDirectory(string directory, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(directory))
        {
            mLogger.LogInformation($"Subscribed directory '{directory}' does not exist");
            return null;
        }

        List<string> backupSettingsFiles = GetSettingsFileFromDirectoryInternal(directory, $"*{Consts.SettingsFileName}", 2);
        if (backupSettingsFiles.Count == 0)
        {
            mLogger.LogInformation($"Could not find settings file in directory '{directory}'");
            return null;
        }
        
        if (backupSettingsFiles.Count > 1)
        {
            mLogger.LogInformation($"Found more than one setting files in directory '{directory}', taking only the first");
        }

        string backupSettingsFilePath = backupSettingsFiles[0];
        BackupSettings? settings = await TryGetBackupSettingsFromFile(backupSettingsFilePath,
            rootDirectory: directory,
            SourceType.DriveOrDirectory,
            cancellationToken).ConfigureAwait(false);
        
        return settings;
    }

    /// <summary>
    /// <see cref="searchDepth"/> = 0 for TopDirectoryOnly.
    /// </summary>
    private List<string> GetSettingsFileFromDirectoryInternal(string directoryToSearch, string searchPattern, ushort searchDepth)
    {
        ushort currentSearchDepth = 0;
        mLogger.LogTrace($"Searching for '{searchPattern}'. Search depth: {searchDepth}");
        List<string> backupSettingsFiles = Directory.GetFiles(directoryToSearch, searchPattern, SearchOption.TopDirectoryOnly).ToList();

        if (backupSettingsFiles.Count > 0)
        {
            return backupSettingsFiles;
        }

        Queue<string> directoriesQueuePerDepth = new();
        Queue<string> pendingDirectoriesQueue = new();
        pendingDirectoriesQueue.Enqueue(directoryToSearch);
        
        while (currentSearchDepth < searchDepth)
        {
            currentSearchDepth++;

            while (pendingDirectoriesQueue.Count > 0)
            {
                directoriesQueuePerDepth.Enqueue(pendingDirectoriesQueue.Dequeue());
            }
            
            while (directoriesQueuePerDepth.Count > 0)
            {
                List<string> directoriesToSearch = new();
                string currentDirectoryToSearch = directoriesQueuePerDepth.Dequeue();
                foreach (string subDirectory in Directory.GetDirectories(currentDirectoryToSearch, searchPattern: "*", SearchOption.TopDirectoryOnly))
                {
                    directoriesToSearch.Add(subDirectory);
                    pendingDirectoriesQueue.Enqueue(subDirectory);
                }

                foreach (string directory in directoriesToSearch)
                {
                    string[] currentBackupSettingsFiles = Directory.GetFiles(directory, searchPattern, SearchOption.TopDirectoryOnly);
                    backupSettingsFiles.AddRange(currentBackupSettingsFiles);
                }    
            }
        }

        return backupSettingsFiles;
    }
    
    private async Task<BackupSettings?> TryGetBackupSettingsFromFile(string backupSettingsFilePath,
        string rootDirectory,
        SourceType sourceType,
        CancellationToken cancellationToken)
    {
        try
        {
            BackupSerializedSettings backupSerializedSettings =
                await mJsonSerializer.DeserializeAsync<BackupSerializedSettings>(backupSettingsFilePath, cancellationToken).ConfigureAwait(false);
            
            return new BackupSettings(backupSerializedSettings, rootDirectory)
            {
                SourceType = sourceType
            };
        }
        catch (Exception ex)
        {
            mLogger.LogError(ex, $"Failed to deserialize '{backupSettingsFilePath}'");
            return null;
        }
    }
}