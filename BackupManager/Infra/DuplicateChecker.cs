using Backuper.App;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Backuper.Infra
{
    public class DuplicateChecker : IDuplicateChecker
    {
        public void WriteDuplicateFiles(string rootDirectory, string duplicatesFilesTxtFile)
        {
            if (!Directory.Exists(Path.GetDirectoryName(duplicatesFilesTxtFile)))
            {
                Console.WriteLine($"{Path.GetDirectoryName(duplicatesFilesTxtFile)} does not exists");
                return;
            }

            WriteDuplicateFiles(FindDuplicateFiles(rootDirectory), duplicatesFilesTxtFile);
        }

        public Dictionary<string, List<string>> FindDuplicateFiles(string rootDirectory)
        {
            Dictionary<string, List<string>> duplicatesFiles = new Dictionary<string, List<string>>();

            if (!Directory.Exists(rootDirectory))
            {
                Console.WriteLine($"{rootDirectory} does not exists");
                return duplicatesFiles;
            }

            Console.WriteLine($"Start recursive operation for finding duplicate files from {rootDirectory}");

            Queue<string> directoriesToSearch = new Queue<string>();
            directoriesToSearch.Enqueue(rootDirectory);

            while (directoriesToSearch.Count > 0)
            {
                string currentSearchDirectory = directoriesToSearch.Dequeue();
                Console.WriteLine($"Collecting from {currentSearchDirectory}");

                // Adding subdirectories to search.
                foreach (string directory in Directory.EnumerateDirectories(currentSearchDirectory))
                {
                    directoriesToSearch.Enqueue(directory);
                }

                // Search files.
                foreach (string filePath in Directory.EnumerateFiles(currentSearchDirectory))
                {
                    AddFileHash(duplicatesFiles, GetFileHash(filePath), filePath);
                }
            }

            Console.WriteLine($"Finished recursive operation for finding duplicate files from {rootDirectory}");
            return duplicatesFiles;
        }

        private string GetFileHash(string filePath)
        {
            using MD5 md5 = MD5.Create();
            using Stream stream = File.OpenRead(filePath);
            byte[] hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }

        private void AddFileHash(Dictionary<string, List<string>> duplicatesFiles, string fileHash, string filePath)
        {
            if (duplicatesFiles.TryGetValue(fileHash, out List<string> paths))
            {
                Console.WriteLine($"Hash {fileHash} found duplicate with file {filePath}");
                paths.Add(filePath);
            }
            else
            {
                duplicatesFiles.Add(fileHash, new List<string>() { filePath });
            }
        }

        private void WriteDuplicateFiles(Dictionary<string, List<string>> duplicatedFiles, string outputPath)
        {
            using StreamWriter writer = File.CreateText(outputPath);
            foreach (KeyValuePair<string, List<string>> keyValuePair in duplicatedFiles)
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