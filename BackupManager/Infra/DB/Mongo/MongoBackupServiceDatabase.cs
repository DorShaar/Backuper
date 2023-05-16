using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.App.Database;
using BackupManager.Infra.DB.Models;
using BackupManager.Infra.DB.Mongo.Extensions;
using BackupManager.Infra.DB.Mongo.Models;
using BackupManager.Infra.DB.Mongo.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BackupManager.Infra.DB.Mongo;

public class MongoBackupServiceDatabase : IBackedUpFilesDatabase
{
	private readonly IMongoCollection<MongoBackedUpFile> mBackupFilesCollection;
	
	public MongoBackupServiceDatabase(IOptions<MongoBackupServiceDatabaseSettings> mongoDatabaseSettings)
	{
		MongoClient mongoClient = new(mongoDatabaseSettings.Value.ConnectionString);
		IMongoDatabase? mongoDatabase = mongoClient.GetDatabase(mongoDatabaseSettings.Value.DatabaseName);
		
		mBackupFilesCollection = mongoDatabase.GetCollection<MongoBackedUpFile>(mongoDatabaseSettings.Value.BackupFilesCollectionName);
	}

	public async Task<IEnumerable<BackedUpFile>> GetAll(CancellationToken cancellationToken)
	{
		IAsyncCursor<MongoBackedUpFile> findResult = await mBackupFilesCollection.FindAsync(_ => true, cancellationToken: cancellationToken).ConfigureAwait(false);
		List<MongoBackedUpFile> mongoBackedUpFiles = await findResult.ToListAsync(cancellationToken).ConfigureAwait(false);

		return convertMongoBackedUpFilesToBackedUpFiles(mongoBackedUpFiles);
	}

	public async Task Insert(BackedUpFile itemToInsert, CancellationToken cancellationToken)
	{
		ReplaceOptions replaceOptions = new()
		{
			IsUpsert = true,
		};

		FilterDefinition<MongoBackedUpFile> hashFilter = Builders<MongoBackedUpFile>.Filter.Eq(file => file.FileHash, itemToInsert.FileHash);
		FilterDefinition<MongoBackedUpFile> pathFilter = Builders<MongoBackedUpFile>.Filter.Eq(file => file.FilePath, itemToInsert.FilePath);
		FilterDefinition<MongoBackedUpFile> hashAndPathFilter = Builders<MongoBackedUpFile>.Filter.And(hashFilter, pathFilter);

		await mBackupFilesCollection.ReplaceOneAsync(hashAndPathFilter,
													 replacement: itemToInsert.ToMongoBackedUpFile(),
													 replaceOptions,
													 cancellationToken: cancellationToken).ConfigureAwait(false);	
	}

	public Task Save(CancellationToken cancellationToken)
	{
		// Does nothing, insert already saves the new item in database.
		return Task.CompletedTask;
	}

	public async Task<IEnumerable<BackedUpFile>?> Find(BackedUpFileSearchModel searchParameter, CancellationToken cancellationToken)
	{
		IAsyncCursor<MongoBackedUpFile>? findResult = null;
		if (!string.IsNullOrWhiteSpace(searchParameter.Id))
		{
			ObjectId objectId = ObjectId.Parse(searchParameter.Id);
			findResult = await mBackupFilesCollection.FindAsync(backedUpFile => backedUpFile.Id == objectId, cancellationToken: cancellationToken).ConfigureAwait(false); 
		}
		
		if (!string.IsNullOrWhiteSpace(searchParameter.FileHash))
		{
			findResult = await mBackupFilesCollection.FindAsync(backedUpFile => backedUpFile.FileHash == searchParameter.FileHash, cancellationToken: cancellationToken).ConfigureAwait(false);
		}
		
		if (!string.IsNullOrWhiteSpace(searchParameter.FilePath))
		{
			findResult = await mBackupFilesCollection.FindAsync(backedUpFile => backedUpFile.FilePath == searchParameter.FilePath, cancellationToken: cancellationToken).ConfigureAwait(false);
		}

		if (findResult is null)
		{
			return null;
		}
		List<MongoBackedUpFile> mongoBackedUpFiles = await findResult.ToListAsync(cancellationToken).ConfigureAwait(false);
		return convertMongoBackedUpFilesToBackedUpFiles(mongoBackedUpFiles);
	}

	private List<BackedUpFile> convertMongoBackedUpFilesToBackedUpFiles(List<MongoBackedUpFile> mongoBackedUpFiles)
	{
		List<BackedUpFile> backedUpFiles = new();
		foreach (MongoBackedUpFile mongoBackedUpFile in mongoBackedUpFiles)
		{
			backedUpFiles.Add(mongoBackedUpFile.ToBackedUpFile());
		}

		return backedUpFiles;
	}
}