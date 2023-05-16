using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BackupManager.Infra.DB.Mongo.Models;

public class MongoBackedUpFile
{
	[BsonId]
	[BsonRepresentation(BsonType.ObjectId)]
	[BsonIgnoreIfDefault]
	public ObjectId? Id { get; set; }
	
	[BsonElement("FilePath")]
	public required string FilePath { get; init; }
	
	[BsonElement("FileHash")]
	public required string FileHash { get; init; }

	[BsonElement("BackupTime")]
	public string? BackupTime { get; init; }
}