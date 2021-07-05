using Backuper.Domain.Mapping;
using System;
using System.Collections.Generic;

namespace Backuper.Domain.Configuration
{
    public class BackuperConfiguration
    {
        public string DriveRootDirectory { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public List<DirectoriesMap> DirectoriesCouples { get; set; }
    }
}