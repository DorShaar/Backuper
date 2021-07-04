using Backuper.App.Serialization;
using Backuper.App;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Backuper.Domain.Configuration;

namespace Backuper.Infra
{
    public class FilesHashesHandler
    {
        private readonly IObjectSerializer mSerializer;
        private readonly IDuplicateChecker mDuplicateChecker;
        private readonly IOptions<BackuperConfiguration> mConfiguration;

        // Key: hash, Value: List of filePaths with same hash.
        public Dictionary<string, List<string>> HashToFilePathDict { get; private set; }
            = new Dictionary<string, List<string>>();

        public FilesHashesHandler(IDuplicateChecker duplicateChecker,
            IObjectSerializer serializer,
            IOptions<BackuperConfiguration> configuration)
        {
            mDuplicateChecker = duplicateChecker ?? throw new ArgumentNullException(nameof(duplicateChecker));
            mSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            mConfiguration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            HashToFilePathDict = mSerializer.Deserialize<Dictionary<string, List<string>>>(mConfiguration.Value.FileHashesPath);
        }

        public int HashesCount => HashToFilePathDict.Count;

        public bool HashExists(string hash) => HashToFilePathDict.ContainsKey(hash);

        public void FindDuplicatedHashes()
        {
            HashToFilePathDict = mDuplicateChecker.FindDuplicateFiles(mConfiguration.Value.BackupRootDirectory);
        }

        public static string GetFileHash(string filePath)
        {
            using MD5 md5 = MD5.Create();
            using Stream stream = File.OpenRead(filePath);
            byte[] hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", string.Empty);
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

        public void Save()
        {
            string savedFilePath = mConfiguration.Value.FileHashesPath;

            // Serialize all hashes.
            mSerializer.Serialize(HashToFilePathDict, savedFilePath);

            // Serialize duplicates hashes files.
            string savedDuplicatesOnlyFilePath = Path.Combine(
                Path.GetDirectoryName(savedFilePath), "dup_only" + Path.GetExtension(savedFilePath));

            Dictionary<string, List<string>> duplicatesOnly = new Dictionary<string, List<string>>();
            foreach (KeyValuePair<string, List<string>> pair in HashToFilePathDict)
            {
                if (pair.Value.Count > 1)
                    duplicatesOnly.Add(pair.Key, pair.Value);
            }

            mSerializer.Serialize(duplicatesOnly, savedDuplicatesOnlyFilePath);
        }
    }
}