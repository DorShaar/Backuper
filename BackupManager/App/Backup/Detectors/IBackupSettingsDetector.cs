using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.Domain.Settings;

namespace BackupManager.App.Backup.Detectors;

public interface IBackupSettingsDetector
{
	Task<List<BackupSettings>?> DetectBackupSettings(CancellationToken cancellationToken);
}