using System;
using System.Collections.Generic;

namespace BackupManager.Domain.Configuration;

public class BackupServiceConfiguration
{
    public List<string>? SubscribedDirectories { get; set; }
    
    public TimeSpan CheckForBackupSettingsInterval { get; set; }
}
