using Serializer.Interface;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace FileHashes
{
    public class FilesHashesHandler : IEnumerable<KeyValuePair<string, List<string>>>
    {
        // Key: hash, Value: List of filePaths with same hash.
        private Dictionary<string, List<string>> mHashToFilePathDict = new Dictionary<string, List<string>>();

        public int Count => mHashToFilePathDict.Count;

        public bool TryAddFileHash(string filePath, bool addIfHashExist = false)
        {
            string hash = GetFileHash(filePath);
            if (mHashToFilePathDict.TryGetValue(hash, out List<string> paths))
            {
                Console.WriteLine($"Hash {hash} found duplicate with file {filePath}");
                if (addIfHashExist)
                    paths.Add(filePath);

                return false;
            }
            else
            {
                mHashToFilePathDict.Add(hash, new List<string>() { filePath });
                return true;
            }
        }

        private string GetFileHash(string filePath)
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
        }

        public void Load(IObjectSerializer serializer, string savedFilePath)
        {
            mHashToFilePathDict = serializer.Deserialize<Dictionary<string, List<string>>>(savedFilePath);
        }
    }
}