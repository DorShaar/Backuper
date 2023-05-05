﻿using System;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.Domain.Settings;

namespace BackupManager.App;

public interface IBackupService : IDisposable
{
    Task BackupFiles(BackupSettings backupSettings, CancellationToken cancellationToken);
}
