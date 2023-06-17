using System;
using System.Collections.Generic;
using BackupManager.Domain.Enums;
using BackupManager.Domain.Mapping;
using BackupManager.Infra;

namespace BackupManager.Domain.Settings;

public class BackupSettings
{
    private readonly BackupSerializedSettings mBackupSerializedSettings;

    public BackupSettings(BackupSerializedSettings backupSerializedSettings, string rootDirectory)
    {
        foreach (DirectoriesMap directoriesMap in backupSerializedSettings.DirectoriesSourcesToDirectoriesDestinationMap)
        {
            if (string.IsNullOrWhiteSpace(directoriesMap.SourceRelativeDirectory))
            {
                if (backupSerializedSettings.ShouldBackupToKnownDirectory)
                {
                    throw new ArgumentException(
                                                $"{nameof(directoriesMap.SourceRelativeDirectory)} should not be empty while" +
                                                $"{nameof(backupSerializedSettings.ShouldBackupToKnownDirectory)} is true");
                }

                directoriesMap.SourceRelativeDirectory = Consts.ReadyToBackupDirectoryPath;
            }
            
        }
        
        mBackupSerializedSettings = backupSerializedSettings;
        RootDirectory = string.IsNullOrWhiteSpace(mBackupSerializedSettings.RootDirectory) ? rootDirectory : mBackupSerializedSettings.RootDirectory;
    }

    public string? Description => mBackupSerializedSettings.Description;

    public List<DirectoriesMap> DirectoriesSourcesToDirectoriesDestinationMap =>
        mBackupSerializedSettings.DirectoriesSourcesToDirectoriesDestinationMap; 

    public bool ShouldBackupToKnownDirectory => mBackupSerializedSettings.ShouldBackupToKnownDirectory;

    public bool AllowMultithreading => mBackupSerializedSettings.AllowMultithreading;

    public ushort SaveInterval => mBackupSerializedSettings.SaveInterval;

    public string? Token => mBackupSerializedSettings.Token;
        
    public SearchMethod SearchMethod { get; set; } = SearchMethod.Hash;

    public SourceType SourceType { get; init; }

    /// <summary>
    /// The name of the media device connected to the computer. 
    /// </summary>
    public string? MediaDeviceName { get; set; }

    /// <summary>
    /// if <see cref="ShouldBackupToKnownDirectory"/> is true, this is the root directory to copy from.
    /// if <see cref="ShouldBackupToKnownDirectory"/> is false, this is the directory to copy to.
    /// </summary>
    public string RootDirectory { get; }

    public override string ToString()
    {
        return $@"BackupSettings:
{nameof(Description)}: {Description}
{nameof(SourceType)}: {SourceType}
{nameof(SearchMethod)}: {SearchMethod}
{nameof(ShouldBackupToKnownDirectory)}: {ShouldBackupToKnownDirectory}
{nameof(MediaDeviceName)}: {MediaDeviceName}
{nameof(RootDirectory)}: {RootDirectory}
{nameof(AllowMultithreading)}: {AllowMultithreading}";
    }
}