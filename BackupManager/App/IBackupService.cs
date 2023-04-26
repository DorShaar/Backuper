using System;
using System.Threading;
using BackupManager.Domain.Settings;

namespace BackupManager.App;

public interface IBackupService : IDisposable
{
    void BackupFiles(BackupSettings backupSettings, CancellationToken cancellationToken);
}
