using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Backuper.Domain.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BackupManager.Infra;

public class BackupOptionsDetector
{
    private readonly IEnumerable<string>? mDirectoriesToListenTo;
    private readonly ILogger<BackupOptionsDetector> mLogger;

    public BackupOptionsDetector(IOptions<BackuperConfiguration> configuration,
        ILogger<BackupOptionsDetector> logger)
    {
        mDirectoriesToListenTo = configuration.Value.SubscribedDirectories;
        mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public IEnumerable<string>? DetectBackupOptions()
    {
        List<string>? settingsFiles = TryGetSettingsFilesFromDrives();
        if (settingsFiles is null)
        {
            return mDirectoriesToListenTo;
        }

        IEnumerable<string>? settingsFilesFromSubscribedDirectories = TryGetSettingsFilesFromSubscribedDirectories();
        if (settingsFilesFromSubscribedDirectories is not null)
        {
            settingsFiles.AddRange(settingsFilesFromSubscribedDirectories);    
        }

        return settingsFiles;
    }

    private List<string>? TryGetSettingsFilesFromDrives()
    {
        List<string>? settingFiles = null;
        
        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType != DriveType.Removable)
            {
                continue;
            }
            
            mLogger.LogInformation($"Detected drive {drive.Name}");

            string[] backupSettingsFiles = Directory.GetFiles(drive.Name, $"*.{Consts.SettingsFileName}", SearchOption.AllDirectories);
            if (backupSettingsFiles.Length == 0)
            {
                mLogger.LogInformation($"No backup settings file detected in drive '{drive.Name}'");
                continue;
            }

            if (backupSettingsFiles.Length > 1)
            {
                mLogger.LogInformation($"Found more than one backup settings files in '{drive.Name}'. Taking the first");
            }

            settingFiles ??= new List<string>();
            settingFiles.Add(backupSettingsFiles[0]);
        }

        return settingFiles;
    }

    private IEnumerable<string>? TryGetSettingsFilesFromSubscribedDirectories()
    {
        // TODO DOR make sure exists and has setting files.
    }
}