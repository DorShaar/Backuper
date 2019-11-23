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
            serializer.Serialize(mHashToFilePathDict, savedFilePath);

            string savedDuplicatesOnlyFilePath = Path.Combine(
                Path.GetDirectoryName(savedFilePath), "dup_only" + Path.GetExtension(savedFilePath));
            var duplicatesOnly = (from pair in mHashToFilePathDict
                                  where pair.Value.Count > 2
                                  select pair);
            serializer.Serialize(duplicatesOnly, savedDuplicatesOnlyFilePath);
        }

        public void Load(IObjectSerializer serializer, string savedFilePath)
        {
            mHashToFilePathDict = serializer.Deserialize<Dictionary<string, List<string>>>(savedFilePath);
        }
    }
}