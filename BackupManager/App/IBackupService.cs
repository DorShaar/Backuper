using System.Threading;
using BackupManager.Domain.Settings;

namespace BackupManager.App;

public interface IBackupService
{
    void BackupFiles(BackupSettings backupSettings, CancellationToken cancellationToken);
}
