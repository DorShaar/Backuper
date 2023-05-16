using BackupManager.Infra.DB.Models;
using BackupManager.Infra.DB.Mongo.Models;
using MongoDB.Bson;

namespace BackupManager.Infra.DB.Mongo.Extensions;

public static class MongoBackedUpFileExtensions
{
	public static MongoBackedUpFile ToMongoBackedUpFile(this BackedUpFile backedUpFile)
	{
		MongoBackedUpFile mongoBackedUpFile = new()
		{
			FileHash = backedUpFile.FileHash,
			FilePath = backedUpFile.FilePath,
			BackupTime = backedUpFile.BackupTime
		};

		if (!string.IsNullOrWhiteSpace(backedUpFile.Id))
		{
			mongoBackedUpFile.Id = ObjectId.Parse(backedUpFile.Id);
		}

		return mongoBackedUpFile;
	}
}