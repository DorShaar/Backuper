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
	private readonly FileSystemPath mDataFilePath = new(Consts.DataFilePath);
	private readonly IJsonSerializer mSerializer;
	private readonly Lazy<ConcurrentDictionary<string, List<string>>> mHashToFilePathsMap;
	private readonly Lazy<ConcurrentDictionary<string, string>> mFilePathToFileHashMap;
	private readonly ILogger<LocalJsonDatabase> mLogger;
	
	public LocalJsonDatabase(IJsonSerializer serializer, ILogger<LocalJsonDatabase> logger)
	{
		mSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
		mLogger = logger ?? throw new ArgumentNullException(nameof(logger));

		mHashToFilePathsMap = new Lazy<ConcurrentDictionary<string, List<string>>>(TryReadHashToFilePathsMap);

		mFilePathToFileHashMap = new Lazy<ConcurrentDictionary<string, string>>(() => DeduceFilePathToFileHashMap(mHashToFilePathsMap.Value));
	}

	public Task<IEnumerable<BackedUpFile>> GetAll(CancellationToken cancellationToken)
	{
		List<BackedUpFile> backedUpFiles = new();
		foreach ((string filePath, string fileHash) in mFilePathToFileHashMap.Value)
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
		if (mHashToFilePathsMap.Value.TryGetValue(itemToInsert.FileHash, out List<string>? paths))
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
			mHashToFilePathsMap.Value.TryAdd(itemToInsert.FileHash, new List<string> { itemToInsert.FilePath });
		}
            
		_ = mFilePathToFileHashMap.Value.TryAdd(itemToInsert.FilePath, itemToInsert.FileHash);
		return Task.CompletedTask;
	}

	public async Task Save(CancellationToken cancellationToken)
	{
		mLogger.LogInformation($"Saving hash to file paths data to '{Consts.DataFilePath}'");
		await mSerializer.SerializeAsync(mHashToFilePathsMap.Value, mDataFilePath.PathString, cancellationToken).ConfigureAwait(false);
	}

	public Task<IEnumerable<BackedUpFile>?> Find(BackedUpFileSearchModel searchParameter, CancellationToken cancellationToken)
	{
		if (!string.IsNullOrWhiteSpace(searchParameter.FileHash))
		{
			return Task.FromResult(FindByHash(searchParameter.FileHash));
		}

		if (!string.IsNullOrWhiteSpace(searchParameter.FilePath))
		{
			return Task.FromResult(FindByPath(searchParameter.FilePath));
		}

		return Task.FromResult<IEnumerable<BackedUpFile>?>(null);
	}

	private IEnumerable<BackedUpFile>? FindByHash(string fileHash)
	{
		if (!mHashToFilePathsMap.Value.TryGetValue(fileHash, out List<string>? filePaths))
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

	private IEnumerable<BackedUpFile>? FindByPath(string filePath)
	{
		if (!mFilePathToFileHashMap.Value.TryGetValue(filePath, out string? fileHash))
		{
			return null;
		}
		
		return new BackedUpFile[] { new()
		{
			FileHash = fileHash,
			FilePath = filePath
		}};
	}

	private ConcurrentDictionary<string, List<string>> TryReadHashToFilePathsMap()
	{
		try
		{
			return File.Exists(Consts.DataFilePath) 
					   ? mSerializer.DeserializeAsync<ConcurrentDictionary<string, List<string>>>(Consts.DataFilePath, CancellationToken.None).Result
					   : new ConcurrentDictionary<string, List<string>>();
		}
		catch (Exception ex)
		{
			mLogger.LogError(ex, $"Failed to deserialize '{Consts.DataFilePath}', initializing new hash map to file path dictionary");
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
		string savedFileDirectory = Path.GetDirectoryName(savedFilePath)
									?? throw new NullReferenceException($"Directory of '{savedFilePath}' is empty"); 
            
		string savedOnlyDuplicatesFilePath = Path.Combine(savedFileDirectory, "dup_only" + Path.GetExtension(savedFilePath));

		Dictionary<string, List<string>> duplicatesOnly = new();
		foreach ((string fileHash, List<string> filesPaths) in mHashToFilePathsMap.Value)
		{
			if (filesPaths.Count > 1)
			{
				duplicatesOnly.Add(fileHash, filesPaths);
			}
		}

		await mSerializer.SerializeAsync(duplicatesOnly, savedOnlyDuplicatesFilePath, cancellationToken);
	}
}