namespace BackupManager.Infra.DB.Mongo.Settings;

public class MongoBackupServiceDatabaseSettings
{
	public string? ConnectionString { get; set; }

	public string? DatabaseName { get; set; }

	public string? BackupFilesCollectionName { get; set; }
}