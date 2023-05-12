using System.Collections.Generic;
using BackupManager.Domain.Mapping;

namespace BackupManager.Domain.Settings;

public class BackupSerializedSettings
{
    public string? Description { get; init; }
        
    public required List<DirectoriesMap> DirectoriesSourcesToDirectoriesDestinationMap { get; init; }
    
    /// <summary>
    /// If True, backups from a location in the root directory to the known backup directory.
    /// If False, backups from known backup directory to a location in the root directory.
    /// </summary>
    public bool ShouldBackupToKnownDirectory { get; init; } = true;

    public bool AllowMultithreading { get; init; } = true;

    /// <summary>
    /// Save files and hashes after <see cref="SaveInterval"/> files. 
    /// </summary>
    public ushort SaveInterval { get; init; } = 100;
}