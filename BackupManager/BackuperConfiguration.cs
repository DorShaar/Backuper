using BackuperApp;
using System;
using System.Collections.Generic;

namespace BackupManager
{
    public class BackuperConfiguration
    {
        public DateTime LastUpdateTime { get; set; }
        public string FileHashesPath { get; set; }
        public List<DirectoriesCouple> DirectoriesCouples { get; set; }
        public string BackupRootDirectory { get; set; }
    }
}