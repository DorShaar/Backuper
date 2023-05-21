using System.Collections.Generic;
using BackupManager.Domain.Enums;
using BackupManager.Domain.Mapping;

namespace BackupManager.Domain.Settings;

public class BackupSettings
{
    private readonly BackupSerializedSettings mBackupSerializedSettings;
    
    public BackupSettings(BackupSerializedSettings backupSerializedSettings)
    {
        mBackupSerializedSettings = backupSerializedSettings;
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
    /// The root directory to copy from.
    /// </summary>
    public string RootDirectory { get; init; } = string.Empty;

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