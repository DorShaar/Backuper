using Backuper.App.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Options;
using Backuper.Domain.Configuration;
using BackupManager.Infra;
using BackupManager.Infra.Hash;
using Microsoft.Extensions.Logging;

namespace Backuper.Infra
{
    public class FilesHashesHandler
    {
        private readonly IObjectSerializer mSerializer;
        private readonly string mHashesFilePath;
        private readonly Dictionary<string, List<string>> mHashToFilePathsMap;
        private readonly Dictionary<string, string> mFilePathToFileHashMap;
        private readonly ILogger<FilesHashesHandler> mLogger;

        public FilesHashesHandler(IObjectSerializer serializer,
            IOptions<BackuperConfiguration> configuration,
            ILogger<FilesHashesHandler> logger)
        {
            mSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            string rootDirectory = configuration.Value.RootDirectory ?? throw new NullReferenceException(nameof(configuration.Value.RootDirectory));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
            mHashesFilePath = Path.Combine(rootDirectory, Consts.HashesFileName);
            
            mHashToFilePathsMap = mSerializer.Deserialize<Dictionary<string, List<string>>>(Path.Combine(rootDirectory, Consts.HashesFileName));
            mFilePathToFileHashMap = deduceFilePathToFileHashMap(mHashToFilePathsMap);
        }

        public int HashesCount => mHashToFilePathsMap.Count;

        public bool HashExists(string hash) => mHashToFilePathsMap.ContainsKey(hash);

        // tOdO DOR think if needed.
        // public void UpdateDuplicatedHashes()
        // {
        //     mHashToFilePathsMap = mDuplicateChecker.FindDuplicateFiles(mRootDirectory);
        // }

        // tOdO DOR think if needed.
        // public void UpdateUnregisteredHashes()
        // {
        //     mHashToFilePathsMap = mUnregisteredHashesAdder.UpdateUnregisteredFiles(HashToFilePathDict);
        // }

        public void AddFileHash(string fileHash, string filePath)
        {
            if (mHashToFilePathsMap.TryGetValue(fileHash, out List<string>? paths))
            {
                mLogger.LogDebug($"File '{filePath}' with Hash {fileHash} has duplicates: '{string.Join(',', paths)}'");
                paths.Add(filePath);
            }
            else
            {
                mHashToFilePathsMap.Add(fileHash, new List<string> { filePath });
            }
            
            _ = mFilePathToFileHashMap.TryAdd(filePath, fileHash);
        }

        public (string fileHash, bool isFileHashExist) IsFileHashExist(string filePath)
        {
            string fileHash = HashCalculator.CalculateHash(filePath);
            return (fileHash, HashExists(fileHash));
        }

        // TOdO DOR think is needed.
        public void Save()
        {
            mSerializer.Serialize(mHashToFilePathsMap, mHashesFilePath);
            WriteOnlyDuplicatesFiles(mHashesFilePath);
        }

        private void WriteOnlyDuplicatesFiles(string savedFilePath)
        {
            string savedFileDirectory = Path.GetDirectoryName(savedFilePath) ?? throw new NullReferenceException($"Directory of '{savedFilePath}' is empty"); 
            
            string savedOnlyDuplicatesFilePath = Path.Combine(savedFileDirectory, "dup_only" + Path.GetExtension(savedFilePath));

            Dictionary<string, List<string>> duplicatesOnly = new();
            foreach ((string fileHash, List<string> filesPaths) in mHashToFilePathsMap)
            {
                if (filesPaths.Count > 1)
                {
                    duplicatesOnly.Add(fileHash, filesPaths);
                }
            }

            mSerializer.Serialize(duplicatesOnly, savedOnlyDuplicatesFilePath);
        }

        private Dictionary<string, string> deduceFilePathToFileHashMap(Dictionary<string, List<string>> fileHashToFilePathsMap)
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