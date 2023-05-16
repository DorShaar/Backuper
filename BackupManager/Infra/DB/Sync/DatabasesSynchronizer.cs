using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.Infra.DB.LocalJsonFileDatabase;
using BackupManager.Infra.DB.Models;
using BackupManager.Infra.DB.Mongo;
using Microsoft.Extensions.Logging;

namespace BackupManager.Infra.DB.Sync;

public class DatabasesSynchronizer
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
	
	public async Task SyncDatabases(CancellationToken cancellationToken)
	{
		await SyncLocalJsonDatabaseFromMongoDatabase(mLocalJsonDatabase, mMongoBackupServiceDatabase, cancellationToken).ConfigureAwait(false);
		await SyncMongoDatabaseFromLocalJsonDatabase(mMongoBackupServiceDatabase, mLocalJsonDatabase, cancellationToken).ConfigureAwait(false);
	}

	private async Task SyncLocalJsonDatabaseFromMongoDatabase(LocalJsonDatabase localJsonDatabase,
																	 MongoBackupServiceDatabase mongoBackupServiceDatabase,
																	 CancellationToken cancellationToken)
	{
		mLogger.LogInformation("Syncing local database with mongo database");
		IEnumerable<BackedUpFile> allBackedUpFiles = await mongoBackupServiceDatabase.GetAll(cancellationToken).ConfigureAwait(false);
		foreach (BackedUpFile backedUpFile in allBackedUpFiles)
		{
			await localJsonDatabase.Insert(backedUpFile, cancellationToken).ConfigureAwait(false);
		}

		await localJsonDatabase.Save(cancellationToken).ConfigureAwait(false);
	}
	
	private async Task SyncMongoDatabaseFromLocalJsonDatabase(MongoBackupServiceDatabase mongoBackupServiceDatabase,
																	 LocalJsonDatabase localJsonDatabase,
																	 CancellationToken cancellationToken)
	{
		mLogger.LogInformation("Syncing mongo database with local database");
		IEnumerable<BackedUpFile> allBackedUpFiles = await localJsonDatabase.GetAll(cancellationToken).ConfigureAwait(false);
		foreach (BackedUpFile backedUpFile in allBackedUpFiles)
		{
			await mongoBackupServiceDatabase.Insert(backedUpFile, cancellationToken).ConfigureAwait(false);
		}
	}
}