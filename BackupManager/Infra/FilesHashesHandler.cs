using Backuper.App.Serialization;
using Backuper.App;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Backuper.Infra
{
    public class FilesHashesHandler
    {
        private readonly IObjectSerializer mSerializer;
        private readonly IDuplicateChecker mDuplicateChecker;

        // Key: hash, Value: List of filePaths with same hash.
        public Dictionary<string, List<string>> HashToFilePathDict { get; private set; }
            = new Dictionary<string, List<string>>();

        public int HashesCount => HashToFilePathDict.Count;

        public bool HashExists(string hash) => HashToFilePathDict.ContainsKey(hash);

        public FilesHashesHandler(IDuplicateChecker duplicateChecker, IObjectSerializer serializer)
        {
            mDuplicateChecker = duplicateChecker;
            mSerializer = serializer;
        }

        public void FindDuplicatedHashes(string rootDirectory)
        {
            HashToFilePathDict = mDuplicateChecker.FindDuplicateFiles(rootDirectory);
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

        public void Save(string savedFilePath)
        {
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

        public void Load(string savedFilePath)
        {
            HashToFilePathDict = mSerializer.Deserialize<Dictionary<string, List<string>>>(savedFilePath);
        }
    }
}