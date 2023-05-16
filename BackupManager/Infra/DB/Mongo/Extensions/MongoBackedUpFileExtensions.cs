using BackupManager.Infra.DB.Models;
using BackupManager.Infra.DB.Mongo.Models;

namespace BackupManager.Infra.DB.Mongo.Extensions;

public static class MongoBackedUpFileExtensions
{
	public static MongoBackedUpFile ToMongoBackedUpFile(this BackedUpFile backedUpFile)
	{
		if (backedUpFile is MongoBackedUpFile mongoBackedUpFile)
		{
			return mongoBackedUpFile;
		}
		
		mongoBackedUpFile = new MongoBackedUpFile
		{
			FileHash = backedUpFile.FileHash,
			FilePath = backedUpFile.FilePath
		};

		if (!string.IsNullOrWhiteSpace(backedUpFile.Id))
		{
			mongoBackedUpFile.Id = backedUpFile.Id;
		}

		return mongoBackedUpFile;
	}
}