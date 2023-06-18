using System.Collections.Generic;
using IOWrapper;

namespace BackupManager.Infra.Backup;

public class FilesBackupData
{
	public required Dictionary<FileSystemPath, string>? AlreadyBackedUpFilePathToFileHashMap { get; init; }
	public required Dictionary<FileSystemPath, string>? NotBackedUpFilePathToFileHashMap { get; init; }
	public required bool IsGetAllFilesCompleted { get; init; }
}