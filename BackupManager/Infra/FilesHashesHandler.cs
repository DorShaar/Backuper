using Backuper.App.Serialization;
using Backuper.App;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Options;
using Backuper.Domain.Configuration;
using BackupManager.Infra;

namespace Backuper.Infra
{
    public class FilesHashesHandler
    {
        private const string HashFileName = "hashes.txt";

        private readonly IObjectSerializer mSerializer;
        private readonly IDuplicateChecker mDuplicateChecker;
        private readonly UnregisteredHashesAdder mUnregisteredHashesAdder;
        private readonly IOptions<BackuperConfiguration> mConfiguration;

        // Key: hash, Value: List of filePaths with same hash.
        public Dictionary<string, List<string>> HashToFilePathDict { get; private set; }
            = new Dictionary<string, List<string>>();

        public FilesHashesHandler(IDuplicateChecker duplicateChecker,
            IObjectSerializer serializer,
            UnregisteredHashesAdder unregisteredHashesAdder,
            IOptions<BackuperConfiguration> configuration)
        {
            mDuplicateChecker = duplicateChecker ?? throw new ArgumentNullException(nameof(duplicateChecker));
            mSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            mUnregisteredHashesAdder = unregisteredHashesAdder ?? throw new ArgumentNullException(nameof(unregisteredHashesAdder));
            mConfiguration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            HashToFilePathDict = mSerializer.Deserialize<Dictionary<string, List<string>>>(Path.Combine(mConfiguration.Value.DriveRootDirectory, HashFileName));
        }

        public int HashesCount => HashToFilePathDict.Count;

        public bool HashExists(string hash) => HashToFilePathDict.ContainsKey(hash);

        public void UpdateDuplicatedHashes()
        {
            HashToFilePathDict = mDuplicateChecker.FindDuplicateFiles(mConfiguration.Value.DriveRootDirectory);
        }

        public void UpdateUnregisteredHashes()
        {
            HashToFilePathDict = mUnregisteredHashesAdder.UpdateUnregisteredFiles(HashToFilePathDict);
        }

        public void AddFileHash(string fileHash, string filePath)
        {
            if (HashToFilePathDict.TryGetValue(fileHash, out List<string> paths))
            {
                Console.WriteLine($"Hash {fileHash} found duplicate with file {filePath}");
                paths.Add(filePath);
            }
            else
            {
                HashToFilePathDict.Add(fileHash, new List<string>() { filePath });
            }
        }

        public void WriteHashesFiles()
        {
            string hashesFilePath = Path.Combine(mConfiguration.Value.DriveRootDirectory, HashFileName);

            mSerializer.Serialize(HashToFilePathDict, hashesFilePath);
            WriteOnlyDuplicatesFiles(hashesFilePath);
        }

        private void WriteOnlyDuplicatesFiles(string savedFilePath)
        {
            string savedOnlyDuplicatesFilePath = Path.Combine(
                Path.GetDirectoryName(savedFilePath), "dup_only" + Path.GetExtension(savedFilePath));

            Dictionary<string, List<string>> duplicatesOnly = new Dictionary<string, List<string>>();
            foreach (KeyValuePair<string, List<string>> pair in HashToFilePathDict)
            {
                if (pair.Value.Count > 1)
                    duplicatesOnly.Add(pair.Key, pair.Value);
            }

            mSerializer.Serialize(duplicatesOnly, savedOnlyDuplicatesFilePath);
        }
    }
}