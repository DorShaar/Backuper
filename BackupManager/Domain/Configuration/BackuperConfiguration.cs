using Backuper.Domain.Mapping;
using System.Collections.Generic;

namespace Backuper.Domain.Configuration
{
    public class BackuperConfiguration
    {
        public string? RootDirectory { get; set; }
        public List<DirectoriesMap>? DirectoriesSourcesToDirectoriesDestinationMap { get; }
    }
}