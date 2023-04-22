using System.Collections.Generic;
using BackupManager.Domain.Mapping;

namespace BackupManager.Domain.Settings;

public class BackupSettings
{
    /// <summary>
    /// If True, backups from a location in the root directory to the known backup directory.
    /// If False, backups from known backup directory to a location in the root directory.
    /// </summary>
    public bool ShouldBackupToKnownDirectory { get; set; } = true;
            
    public string? RootDirectory { get; set; }
        
    public required List<DirectoriesMap> DirectoriesSourcesToDirectoriesDestinationMap { get; set; }
}