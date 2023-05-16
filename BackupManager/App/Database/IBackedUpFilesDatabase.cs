using BackupManager.Infra.DB.Models;

namespace BackupManager.App.Database;

public interface IBackedUpFilesDatabase: IDatabase<BackedUpFile, BackedUpFileSearchModel>
{
}