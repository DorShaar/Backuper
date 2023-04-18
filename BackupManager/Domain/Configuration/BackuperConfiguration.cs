using Backuper.Domain.Mapping;
using System.Collections.Generic;

namespace Backuper.Domain.Configuration
{
    public class BackuperConfiguration
    {
        /// <summary>
        /// If True, backups from a location in the root directory to the known backup directory.
        /// If False, backups from known backup directory to a location in the root directory.
        /// </summary>
        public bool ShouldBackupToKnownDirectory { get; set; } = true;
            
        public string? RootDirectory { get; set; }
        
        public List<DirectoriesMap>? DirectoriesSourcesToDirectoriesDestinationMap { get; set; }
        
        public List<string>? SubscribedDirectories { get; set; }
    }
}