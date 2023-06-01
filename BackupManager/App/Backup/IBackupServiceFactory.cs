using BackupManager.App.Backup.Services;
using BackupManager.Domain.Settings;

namespace BackupManager.App.Backup;

public interface IBackupServiceFactory
{
	IBackupService Create(BackupSettings backupSettings);
}