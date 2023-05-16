using BackupManager.Infra.DB.Models;
using BackupManager.Infra.DB.Mongo.Models;

namespace BackupManager.Infra.DB.Mongo.Extensions;

public static class BackedUpFileExtensions
{
	public static BackedUpFile ToBackedUpFile(this MongoBackedUpFile backedUpFile)
	{
		BackedUpFile mongoBackedUpFile = new()
		{
			FileHash = backedUpFile.FileHash,
			FilePath = backedUpFile.FilePath,
			Id = backedUpFile.Id.ToString(),
			BackupTime = backedUpFile.BackupTime
		};

		return mongoBackedUpFile;
	}
}