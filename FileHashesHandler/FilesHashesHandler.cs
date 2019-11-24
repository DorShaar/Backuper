using Serializer.Interface;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace FileHashes
{
    public class FilesHashesHandler : IEnumerable<KeyValuePair<string, List<string>>>
    {
        // Key: hash, Value: List of filePaths with same hash.
        private Dictionary<string, List<string>> mHashToFilePathDict = new Dictionary<string, List<string>>();

        public int Count => mHashToFilePathDict.Count;

        public bool HashExists(string hash) => mHashToFilePathDict.ContainsKey(hash);

        public void AddFileHash(string fileHash, string filePath)
        {
            if (mHashToFilePathDict.TryGetValue(fileHash, out List<string> paths))
            {
                Console.WriteLine($"Hash {fileHash} found duplicate with file {filePath}");
                paths.Add(filePath);
            }
            else
            {
                mHashToFilePathDict.Add(fileHash, new List<string>() { filePath });
            }
        }

        public string GetFileHash(string filePath)
        {
            using (MD5 md5 = MD5.Create())
            using (Stream stream = File.OpenRead(filePath))
            {
                byte[] hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }

        public IEnumerator<KeyValuePair<string, List<string>>> GetEnumerator()
        {
            return mHashToFilePathDict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Save(IObjectSerializer serializer, string savedFilePath)
        {
            // Serialize all hashes.
            serializer.Serialize(mHashToFilePathDict, savedFilePath);

            // Serialize duplicates hashes files.
            string savedDuplicatesOnlyFilePath = Path.Combine(
                Path.GetDirectoryName(savedFilePath), "dup_only" + Path.GetExtension(savedFilePath));

            Dictionary<string, List<string>> duplicatesOnly = new Dictionary<string, List<string>>();
            foreach (KeyValuePair<string, List<string>> pair in mHashToFilePathDict)
            {
                if (pair.Value.Count > 1)
                    duplicatesOnly.Add(pair.Key, pair.Value);
            }

            serializer.Serialize(duplicatesOnly, savedDuplicatesOnlyFilePath);
        }

        public void Load(IObjectSerializer serializer, string savedFilePath)
        {
            mHashToFilePathDict = serializer.Deserialize<Dictionary<string, List<string>>>(savedFilePath);
        }
    }
}