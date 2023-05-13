using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.App.Database;
using BackupManager.Infra.DB.Models;
using BackupManager.Infra.DB.Mongo.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BackupManager.Infra.DB.Mongo;

public class MongoBackupServiceDatabase : IBackedUpFilesDatabase
{
	private readonly IMongoCollection<BackedUpFile> mBackupFilesCollection;
	
	public MongoBackupServiceDatabase(IOptions<MongoBackupServiceDatabaseSettings> mongoDatabaseSettings)
	{
		MongoClient mongoClient = new(mongoDatabaseSettings.Value.ConnectionString);
		IMongoDatabase? mongoDatabase = mongoClient.GetDatabase(mongoDatabaseSettings.Value.DatabaseName);
		mBackupFilesCollection = mongoDatabase.GetCollection<BackedUpFile>(mongoDatabaseSettings.Value.BackupFilesCollectionName);
	}
	
	public async Task Insert(BackedUpFile itemToInsert, CancellationToken cancellationToken)
	{
		await mBackupFilesCollection.InsertOneAsync(itemToInsert, cancellationToken: cancellationToken).ConfigureAwait(false);	
	}

	public Task Save(CancellationToken cancellationToken)
	{
		// Does nothing, insert already saves the new item in database.
		return Task.CompletedTask;
	}

	public async Task<IEnumerable<BackedUpFile>?> Find(BackedUpFileSearchModel searchParameter, CancellationToken cancellationToken)
	{
		if (!string.IsNullOrWhiteSpace(searchParameter.Id))
		{
			return await mBackupFilesCollection.Find(backedUpFile => backedUpFile.Id == searchParameter.Id)
											   .ToListAsync(cancellationToken)
											   .ConfigureAwait(false);
		}
		
		if (!string.IsNullOrWhiteSpace(searchParameter.FileHash))
		{
			return await mBackupFilesCollection.Find(backedUpFile => backedUpFile.FileHash == searchParameter.FileHash)
											   .ToListAsync(cancellationToken)
											   .ConfigureAwait(false);
		}
		
		if (!string.IsNullOrWhiteSpace(searchParameter.FilePath))
		{
			return await mBackupFilesCollection.Find(backedUpFile => backedUpFile.FilePath == searchParameter.FilePath)
											   .ToListAsync(cancellationToken)
											   .ConfigureAwait(false);
		}

		return null;
	}
}