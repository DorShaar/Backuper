using BackupManager.Infra.DB.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BackupManager.Infra.DB.Mongo.Models;

public class MongoBackedUpFile : BackedUpFile
{
	[BsonId]
	[BsonRepresentation(BsonType.ObjectId)]
	public new string? Id { get; set; }

	[BsonElement("BackupTime")]
	public string? BackupTime { get; set; }
}