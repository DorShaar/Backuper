using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.Infra;
using IOWrapper;
using JsonSerialization;
using Microsoft.Extensions.Logging;

namespace BackupManager.Domain.Hash
{
    public class FilesHashesHandler : IFilesHashesHandler
    {
        private readonly FileSystemPath mDataFilePath = new(Consts.DataFilePath);
        private readonly IJsonSerializer mSerializer;
        private readonly Lazy<ConcurrentDictionary<string, List<string>>> mHashToFilePathsMap;
        private readonly Lazy<ConcurrentDictionary<string, string>> mFilePathToFileHashMap;
        private readonly ILogger<FilesHashesHandler> mLogger;
        
        public FilesHashesHandler(IJsonSerializer serializer, ILogger<FilesHashesHandler> logger)
        {
            mSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));

            mHashToFilePathsMap = new Lazy<ConcurrentDictionary<string, List<string>>>(TryReadHashToFilePathsMap);

            mFilePathToFileHashMap = new Lazy<ConcurrentDictionary<string, string>>(() => DeduceFilePathToFileHashMap(mHashToFilePathsMap.Value));
        }

        public int HashesCount => mHashToFilePathsMap.Value.Count;

        public bool IsHashExists(string hash) => mHashToFilePathsMap.Value.ContainsKey(hash);

        public bool IsFilePathExist(string filePath) => mFilePathToFileHashMap.Value.ContainsKey(filePath);

        public string CalculateHash(string filePath) => HashCalculator.CalculateHash(filePath);

        public void AddFileHash(string fileHash, string filePath)
        {
            if (mHashToFilePathsMap.Value.TryGetValue(fileHash, out List<string>? paths))
            {
                if (paths.Contains(filePath))
                {
                    mLogger.LogDebug($"File '{filePath}' with Hash {fileHash} already exists");
                    return;
                }
                
                mLogger.LogDebug($"File '{filePath}' with Hash {fileHash} has duplicates: '{string.Join(',', paths)}'");
                paths.Add(filePath);
            }
            else
            {
                mHashToFilePathsMap.Value.TryAdd(fileHash, new List<string> { filePath });
            }
            
            _ = mFilePathToFileHashMap.Value.TryAdd(filePath, fileHash);
        }

        public async Task Save(CancellationToken cancellationToken)
        {
            mLogger.LogInformation($"Saving hash to file paths data to '{Consts.DataFilePath}'");
            await mSerializer.SerializeAsync(mHashToFilePathsMap.Value, mDataFilePath.PathString, cancellationToken).ConfigureAwait(false);
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
    }
}