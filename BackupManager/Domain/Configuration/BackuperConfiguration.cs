using Backuper.Domain.Mapping;
using System.Collections.Generic;

namespace Backuper.Domain.Configuration
{
    public class BackuperConfiguration
    {
        /// <summary>
        /// If True, backups from known backup directory to a location in the root directory.
        /// If False, backups from a location in the root directory to the known backup directory.
        /// </summary>
        public bool ShouldBackupToSelf { get; set; }
            
        public string? RootDirectory { get; set; }
        
        public List<DirectoriesMap>? DirectoriesSourcesToDirectoriesDestinationMap { get; set; }
    }
}