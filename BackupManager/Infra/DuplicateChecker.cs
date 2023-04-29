using System;
using System.Collections.Generic;
using System.IO;
using BackupManager.App;
using BackupManager.Domain.Hash;

namespace BackupManager.Infra
{
    public class DuplicateChecker : IDuplicateChecker
    {
        public void WriteDuplicateFiles(string rootDirectory, string duplicateFilesOutputFilePath)
        {
            string? directory = Path.GetDirectoryName(duplicateFilesOutputFilePath);
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                Console.WriteLine($"{directory} does not exists");
                return;
            }

            WriteDuplicateFiles(FindDuplicateFiles(rootDirectory), duplicateFilesOutputFilePath);
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

        public Dictionary<string, List<string>> FindDuplicateFiles(string rootDirectory)
        {
            if (!Directory.Exists(rootDirectory))
            {
                Console.WriteLine($"{rootDirectory} does not exists");
                return new Dictionary<string, List<string>>();
            }

            return FindDuplicateFilesIterative(rootDirectory);
        }

        private Dictionary<string, List<string>> FindDuplicateFilesIterative(string rootDirectory)
        {
            Dictionary<string, List<string>> duplicatesFiles = new Dictionary<string, List<string>>();

            Console.WriteLine($"Start iterative operation for finding duplicate files from {rootDirectory}");

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
                    AddFileHashToGivenDict(duplicatesFiles, HashCalculator.CalculateHash(filePath), filePath);
                }
            }

            Console.WriteLine($"Finished iterative operation for finding duplicate files from {rootDirectory}");
            return duplicatesFiles;
        }

        private void AddFileHashToGivenDict(Dictionary<string, List<string>> duplicatesFiles, string fileHash, string filePath)
        {
            if (duplicatesFiles.TryGetValue(fileHash, out List<string>? paths))
            {
                Console.WriteLine($"Hash {fileHash} found duplicate with file {filePath}");
                paths.Add(filePath);
            }
            else
            {
                duplicatesFiles.Add(fileHash, new List<string>() { filePath });
            }
        }
    }
}