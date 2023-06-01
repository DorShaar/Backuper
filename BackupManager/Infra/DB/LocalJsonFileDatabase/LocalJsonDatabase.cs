using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.App.Database;
using BackupManager.Infra.DB.Models;
using IOWrapper;
using JsonSerialization;
using Microsoft.Extensions.Logging;

namespace BackupManager.Infra.DB.LocalJsonFileDatabase;

public class LocalJsonDatabase : IBackedUpFilesDatabase
{
	private readonly IJsonSerializer mSerializer;
	private readonly ILogger<LocalJsonDatabase> mLogger;
	private ConcurrentDictionary<string, List<string>>? mHashToFilePathsMap;
	private ConcurrentDictionary<string, string>? mFilePathToFileHashMap;
	private FileSystemPath? mDatabaseFilePath;

	public LocalJsonDatabase(IJsonSerializer serializer, ILogger<LocalJsonDatabase> logger)
	{
		mSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
		mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
	}
	
	public async Task Load(string databaseName, CancellationToken cancellationToken)
	{
		if (mHashToFilePathsMap is not null && mHashToFilePathsMap.Any() && !string.IsNullOrWhiteSpace(mDatabaseFilePath?.PathString))
		{
			mLogger.LogInformation($"Before loading database {databaseName}, saving current database into '{mDatabaseFilePath?.PathString}'");
			await Save(cancellationToken).ConfigureAwait(false);
		}
		
		mDatabaseFilePath = new FileSystemPath(Path.Combine(Consts.DataDirectoryPath, $"{databaseName}.{Consts.LocalDatabaseExtension}"));
		mHashToFilePathsMap = TryReadHashToFilePathsMap(mDatabaseFilePath.PathString);
		mFilePathToFileHashMap = DeduceFilePathToFileHashMap(mHashToFilePathsMap);
	}

	public Task<IEnumerable<BackedUpFile>> GetAll(CancellationToken cancellationToken)
	{
		if (mFilePathToFileHashMap is null)
		{
			throw new InvalidOperationException($"Please call method {nameof(Load)} first");
		}
		
		List<BackedUpFile> backedUpFiles = new();
		foreach ((string filePath, string fileHash) in mFilePathToFileHashMap)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				mLogger.LogInformation($"Cancel requested");
				break;
			}
			
			BackedUpFile backedUpFile = new()
			{
				FilePath = filePath,
				FileHash = fileHash 
			};
			backedUpFiles.Add(backedUpFile);
		}

		return Task.FromResult(backedUpFiles.AsEnumerable());
	}

	public Task Insert(BackedUpFile itemToInsert, CancellationToken cancellationToken)
	{
		if (mFilePathToFileHashMap is null || mHashToFilePathsMap is null)
		{
			throw new InvalidOperationException($"Please call method {nameof(Load)} first");
		}
		
		if (mHashToFilePathsMap.TryGetValue(itemToInsert.FileHash, out List<string>? paths))
		{
			if (paths.Contains(itemToInsert.FilePath))
			{
				mLogger.LogDebug($"File '{itemToInsert.FilePath}' with Hash {itemToInsert.FileHash} already exists");
				return Task.CompletedTask;
			}
                
			mLogger.LogDebug($"File '{itemToInsert.FilePath}' with Hash {itemToInsert.FileHash} has duplicates: '{string.Join(',', paths)}'");
			paths.Add(itemToInsert.FilePath);
		}
		else
		{
			mHashToFilePathsMap.TryAdd(itemToInsert.FileHash, new List<string> { itemToInsert.FilePath });
		}
            
		_ = mFilePathToFileHashMap.TryAdd(itemToInsert.FilePath, itemToInsert.FileHash);
		return Task.CompletedTask;
	}

	public async Task Save(CancellationToken cancellationToken)
	{
		if (mDatabaseFilePath is null)
		{
			throw new InvalidOperationException($"Please call method {nameof(Load)} first");
		}
		
		mLogger.LogInformation($"Saving hash to file paths data to '{mDatabaseFilePath}'");
		await mSerializer.SerializeAsync(mHashToFilePathsMap, mDatabaseFilePath.PathString, cancellationToken).ConfigureAwait(false);
	}

	public Task<IEnumerable<BackedUpFile>?> Find(BackedUpFileSearchModel searchParameter, CancellationToken cancellationToken)
	{
		if (mFilePathToFileHashMap is null || mHashToFilePathsMap is null)
		{
			throw new InvalidOperationException($"Please call method {nameof(Load)} first");
		}
		
		if (!string.IsNullOrWhiteSpace(searchParameter.FileHash))
		{
			return Task.FromResult(FindByHash(searchParameter.FileHash, mHashToFilePathsMap));
		}

		if (!string.IsNullOrWhiteSpace(searchParameter.FilePath))
		{
			return Task.FromResult(FindByPath(searchParameter.FilePath, mFilePathToFileHashMap));
		}

		return Task.FromResult<IEnumerable<BackedUpFile>?>(null);
	}

	private IEnumerable<BackedUpFile>? FindByHash(string fileHash, ConcurrentDictionary<string, List<string>> hashToFilePathsMap)
	{
		if (!hashToFilePathsMap.TryGetValue(fileHash, out List<string>? filePaths))
		{
			return null;
		}

		if (filePaths.Count == 0)
		{
			return null;
		}
			
		return filePaths.Select(filePath => new BackedUpFile
		{
			FileHash = fileHash,
			FilePath = filePath
		});
	}

	private IEnumerable<BackedUpFile>? FindByPath(string filePath, ConcurrentDictionary<string, string> filePathToFileHashMap)
	{
		if (!filePathToFileHashMap.TryGetValue(filePath, out string? fileHash))
		{
			return null;
		}
		
		return new BackedUpFile[] { new()
		{
			FileHash = fileHash,
			FilePath = filePath
		}};
	}

	private ConcurrentDictionary<string, List<string>> TryReadHashToFilePathsMap(string databaseFilePath)
	{
		try
		{
			return File.Exists(databaseFilePath) 
					   ? mSerializer.DeserializeAsync<ConcurrentDictionary<string, List<string>>>(databaseFilePath, CancellationToken.None).Result
					   : new ConcurrentDictionary<string, List<string>>();
		}
		catch (Exception ex)
		{
			mLogger.LogError(ex, $"Failed to deserialize '{databaseFilePath}', initializing new hash map to file path dictionary");
			return new ConcurrentDictionary<string, List<string>>();
		}
	}

	private static ConcurrentDictionary<string, string> DeduceFilePathToFileHashMap(ConcurrentDictionary<string, List<string>> fileHashToFilePathsMap)
	{
		ConcurrentDictionary<string, string> filePathToFileHashMap = new();

		foreach ((string fileHash, List<string> filesPaths) in fileHashToFilePathsMap)
		{
			foreach (string filePath in filesPaths)
			{
				_ = filePathToFileHashMap.TryAdd(filePath, fileHash);
			}
		}

		return filePathToFileHashMap;
	}
	
	// ReSharper disable once UnusedMember.Local
	private async Task WriteOnlyDuplicatesFiles(string savedFilePath, CancellationToken cancellationToken)
	{
		if (mHashToFilePathsMap is null)
		{
			throw new InvalidOperationException($"Please call method {nameof(Load)} first");
		}
		
		string savedFileDirectory = Path.GetDirectoryName(savedFilePath)
									?? throw new NullReferenceException($"Directory of '{savedFilePath}' is empty"); 
            
		string savedOnlyDuplicatesFilePath = Path.Combine(savedFileDirectory, "dup_only" + Path.GetExtension(savedFilePath));

		Dictionary<string, List<string>> duplicatesOnly = new();
		foreach ((string fileHash, List<string> filesPaths) in mHashToFilePathsMap)
		{
			if (filesPaths.Count > 1)
			{
				duplicatesOnly.Add(fileHash, filesPaths);
			}
		}

		await mSerializer.SerializeAsync(duplicatesOnly, savedOnlyDuplicatesFilePath, cancellationToken);
	}
}