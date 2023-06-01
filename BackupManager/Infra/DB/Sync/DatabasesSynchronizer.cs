using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.App.Database.Sync;
using BackupManager.Infra.DB.LocalJsonFileDatabase;
using BackupManager.Infra.DB.Models;
using BackupManager.Infra.DB.Mongo;
using Microsoft.Extensions.Logging;

namespace BackupManager.Infra.DB.Sync;

public class DatabasesSynchronizer : IDatabasesSynchronizer 
{
	private readonly LocalJsonDatabase mLocalJsonDatabase;
	private readonly MongoBackupServiceDatabase mMongoBackupServiceDatabase;
	private readonly ILogger<DatabasesSynchronizer> mLogger;

	public DatabasesSynchronizer(LocalJsonDatabase localJsonDatabase,
								 MongoBackupServiceDatabase mongoBackupServiceDatabase,
								 ILogger<DatabasesSynchronizer> logger)
	{
		mLocalJsonDatabase = localJsonDatabase ?? throw new ArgumentNullException(nameof(localJsonDatabase));
		mMongoBackupServiceDatabase = mongoBackupServiceDatabase ?? throw new ArgumentNullException(nameof(mongoBackupServiceDatabase));
		
		mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
	}
	
	public async Task SyncDatabases(IEnumerable<string> knownTokens, CancellationToken cancellationToken)
	{
		string[] collections = knownTokens.Select(token => string.Format(Consts.BackupFilesForKnownDriveCollectionTemplate, token))
										   .Append(Consts.BackupFilesCollectionName)
										   .ToArray();
		await SyncLocalJsonDatabaseFromMongoDatabase(mLocalJsonDatabase, mMongoBackupServiceDatabase, collections,  cancellationToken).ConfigureAwait(false);
		await SyncMongoDatabaseFromLocalJsonDatabase(mMongoBackupServiceDatabase, mLocalJsonDatabase, collections, cancellationToken).ConfigureAwait(false);
	}

	private async Task SyncLocalJsonDatabaseFromMongoDatabase(LocalJsonDatabase localJsonDatabase,
															  MongoBackupServiceDatabase mongoBackupServiceDatabase,
															  IEnumerable<string> collections,
															  CancellationToken cancellationToken)
	{
		foreach (string collectionName in collections)
		{
			await SyncLocalJsonDatabaseFromMongoDatabaseInternal(localJsonDatabase, mongoBackupServiceDatabase, collectionName, cancellationToken).ConfigureAwait(false);
		}
	}

	private async Task SyncLocalJsonDatabaseFromMongoDatabaseInternal(LocalJsonDatabase localJsonDatabase,
																	  MongoBackupServiceDatabase mongoBackupServiceDatabase,
																	  string collectionName,
																	  CancellationToken cancellationToken)
	{
		mLogger.LogInformation($"Syncing local database with mongo database with collection {collectionName}");
		await mongoBackupServiceDatabase.Load(collectionName, cancellationToken).ConfigureAwait(false);
		await localJsonDatabase.Load(collectionName, cancellationToken).ConfigureAwait(false);
		
		IEnumerable<BackedUpFile> allBackedUpFiles = await mongoBackupServiceDatabase.GetAll(cancellationToken).ConfigureAwait(false);
		foreach (BackedUpFile backedUpFile in allBackedUpFiles)
		{
			await localJsonDatabase.Insert(backedUpFile, cancellationToken).ConfigureAwait(false);
		}

		await localJsonDatabase.Save(cancellationToken).ConfigureAwait(false);
	}
	
	private async Task SyncMongoDatabaseFromLocalJsonDatabase(MongoBackupServiceDatabase mongoBackupServiceDatabase,
															  LocalJsonDatabase localJsonDatabase,
															  IEnumerable<string> collections,
															  CancellationToken cancellationToken)
	{
		foreach (string collectionName in collections)
		{
			await SyncMongoDatabaseFromLocalJsonDatabaseInternal(mongoBackupServiceDatabase, localJsonDatabase, collectionName, cancellationToken).ConfigureAwait(false);
		}
	}

	private async Task SyncMongoDatabaseFromLocalJsonDatabaseInternal(MongoBackupServiceDatabase mongoBackupServiceDatabase,
																	  LocalJsonDatabase localJsonDatabase,
																	  string collectionName,
																	  CancellationToken cancellationToken)

	{
		mLogger.LogInformation($"Syncing mongo database with local database with collection {collectionName}");
		await mongoBackupServiceDatabase.Load(collectionName, cancellationToken).ConfigureAwait(false);
		await localJsonDatabase.Load(collectionName, cancellationToken).ConfigureAwait(false);
		
		IEnumerable<BackedUpFile> allBackedUpFiles = await localJsonDatabase.GetAll(cancellationToken).ConfigureAwait(false);
		foreach (BackedUpFile backedUpFile in allBackedUpFiles)
		{
			await mongoBackupServiceDatabase.Insert(backedUpFile, cancellationToken).ConfigureAwait(false);
		}
	}
}