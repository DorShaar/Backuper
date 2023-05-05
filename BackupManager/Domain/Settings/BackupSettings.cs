using System.Collections.Generic;
using System.Text.Json.Serialization;
using BackupManager.Domain.Enums;
using BackupManager.Domain.Mapping;

namespace BackupManager.Domain.Settings;

public class BackupSettings
{
    public string? Description { get; set; }
    
    public SourceType SourceType { get; set; }
    
    [JsonIgnore]
    public SearchMethod SearchMethod { get; set; } = SearchMethod.FilePath; 
    
    /// <summary>
    /// If True, backups from a location in the root directory to the known backup directory.
    /// If False, backups from known backup directory to a location in the root directory.
    /// </summary>
    public bool ShouldBackupToKnownDirectory { get; set; } = true;
    
    /// <summary>
    /// The name of the media device connected to the computer. 
    /// </summary>
    public string? MediaDeviceName { get; set; }
    
    /// <summary>
    /// The root directory to copy from.
    /// </summary>
    public string? RootDirectory { get; set; }
        
    public required List<DirectoriesMap> DirectoriesSourcesToDirectoriesDestinationMap { get; set; }

    public bool AllowMultithreading { get; set; } = true;

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