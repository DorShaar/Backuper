using RecursiveDirectoryEnumaratorClass;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace DuplicateChecker
{
    public class Checker
    {
        public void PrintDuplicateFiles(string rootDirectory, string duplicatesFilesTxtFile)
        {
            if (!Directory.Exists(Path.GetDirectoryName(duplicatesFilesTxtFile)))
            {
                Console.WriteLine($"{Path.GetDirectoryName(duplicatesFilesTxtFile)} does not exists");
                return;
            }

            PrintDuplicateFiles(GetDuplicateFiles(rootDirectory), duplicatesFilesTxtFile);
        }

        public Dictionary<string, List<string>> GetDuplicateFiles(string rootDirectory)
        {
            var directoryEnumarator = new RecursiveDirectoryEnumarator<Dictionary<string, List<string>>>();
            return directoryEnumarator.OperateRecursive(rootDirectory, AddFileHash, "Duplicate Files Search");
        }

        public void AddFileHash(string filePath, Dictionary<string, List<string>> hashToFilePathDict)
        {
            string hash = GetFileHash(filePath);
            if (hashToFilePathDict.TryGetValue(hash, out List<string> paths))
            {
                Console.WriteLine($"Hash {hash} found duplicate with file {filePath}");
                paths.Add(filePath);
            }
            else
            {
                hashToFilePathDict.Add(hash, new List<string>());
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

        private void PrintDuplicateFiles(Dictionary<string, List<string>> hashToFilePath, string outputPath)
        {
            using (StreamWriter writer = File.CreateText(outputPath))
            {
                foreach (KeyValuePair<string, List<string>> keyValuePair in hashToFilePath)
                {
                    if (keyValuePair.Value.Count > 1)
                    {
                        writer.WriteLine($"Duplicate {keyValuePair.Key}");
                        keyValuePair.Value.ForEach(file => writer.WriteLine(file));
                        writer.WriteLine(string.Empty);
                    }
                }
            }
        }
    }
}