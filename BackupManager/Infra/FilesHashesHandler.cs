using System;
using System.Collections.Generic;
using System.IO;
using BackupManager.App.Serialization;
using BackupManager.Infra.Hash;
using Microsoft.Extensions.Logging;

namespace BackupManager.Infra
{
    public class FilesHashesHandler
    {
        private readonly IObjectSerializer mSerializer;
        private readonly Lazy<Dictionary<string, List<string>>> mHashToFilePathsMap;
        private readonly Lazy<Dictionary<string, string>> mFilePathToFileHashMap; // TODO DOR think if needed.
        private readonly ILogger<FilesHashesHandler> mLogger;
        
        public FilesHashesHandler(IObjectSerializer serializer, ILogger<FilesHashesHandler> logger)
        {
            mSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));

            mHashToFilePathsMap = new Lazy<Dictionary<string, List<string>>>(TryReadHashToFilePathsMap);

            mFilePathToFileHashMap = new Lazy<Dictionary<string, string>>(() => DeduceFilePathToFileHashMap(mHashToFilePathsMap.Value));
        }

        public int HashesCount => mHashToFilePathsMap.Value.Count;

        public bool HashExists(string hash) => mHashToFilePathsMap.Value.ContainsKey(hash);

        public void AddFileHash(string fileHash, string filePath)
        {
            if (mHashToFilePathsMap.Value.TryGetValue(fileHash, out List<string>? paths))
            {
                mLogger.LogDebug($"File '{filePath}' with Hash {fileHash} has duplicates: '{string.Join(',', paths)}'");
                paths.Add(filePath);
            }
            else
            {
                mHashToFilePathsMap.Value.Add(fileHash, new List<string> { filePath });
            }
            
            _ = mFilePathToFileHashMap.Value.TryAdd(filePath, fileHash);
        }

        public (string fileHash, bool isFileHashExist) IsFileHashExist(string filePath)
        {
            string fileHash = HashCalculator.CalculateHash(filePath);
            return (fileHash, HashExists(fileHash));
        }

        public void Save()
        {
            mSerializer.Serialize(mHashToFilePathsMap.Value, Consts.DataFilePath);
        }

        // ReSharper disable once UnusedMember.Local
        private void WriteOnlyDuplicatesFiles(string savedFilePath)
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

            mSerializer.Serialize(duplicatesOnly, savedOnlyDuplicatesFilePath);
        }

        private Dictionary<string, List<string>> TryReadHashToFilePathsMap()
        {
            try
            {
                return File.Exists(Consts.DataFilePath) 
                    ? mSerializer.Deserialize<Dictionary<string, List<string>>>(Consts.DataFilePath)
                    : new Dictionary<string, List<string>>();
            }
            catch (Exception ex)
            {
                mLogger.LogError(ex, $"Failed to deserialize '{Consts.DataFilePath}', initializing new hash map to file path dictionary");
                return new Dictionary<string, List<string>>();
            }
        }

        private static Dictionary<string, string> DeduceFilePathToFileHashMap(Dictionary<string, List<string>> fileHashToFilePathsMap)
        {
            Dictionary<string, string> filePathToFileHashMap = new();

            foreach ((string fileHash, List<string> filesPaths) in fileHashToFilePathsMap)
            {
                foreach (string filePath in filesPaths)
                {
                    filePathToFileHashMap.Add(filePath, fileHash);
                }
            }

            return filePathToFileHashMap;
        }
    }
}