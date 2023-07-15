using BackupManagerCore.Hash;
using Newtonsoft.Json;

namespace BackupManagerCli
{
    public static class DuplicateChecker
    {
        public static void WriteDuplicateFiles(Dictionary<string, List<string>> duplicatedFiles, string outputPath)
        {
            using StreamWriter writer = File.CreateText(outputPath);
            foreach ((string fileHash, List<string> filePaths) in duplicatedFiles)
            {
                if (filePaths.Count > 1)
                {
                    writer.WriteLine($"Duplicate hash {fileHash}");
                    filePaths.ForEach(file => writer.WriteLine(file));
                    writer.WriteLine(string.Empty);
                }
            }
        }

        public static Dictionary<string, List<string>> FindDuplicateFiles(string rootDirectory)
        {
            if (!Directory.Exists(rootDirectory))
            {
                Console.WriteLine($"{rootDirectory} does not exists");
                return new Dictionary<string, List<string>>();
            }

            return FindDuplicateFilesIterative(rootDirectory);
        }

        private static Dictionary<string, List<string>> FindDuplicateFilesIterative(string rootDirectory)
        {
            Dictionary<string, List<string>> hashToFilePaths = new();

            Console.WriteLine($"Start iterative operation for finding duplicate files from {rootDirectory}");

            Queue<string> directoriesToSearch = new();
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
                    AddFileHashToGivenDict(hashToFilePaths, HashCalculator.CalculateHash(filePath), filePath);
                }
            }

            Console.WriteLine($"Finished iterative operation for finding duplicate files from {rootDirectory}");

            checkWithDatabase(hashToFilePaths);

            Dictionary<string, List<string>> duplicates = hashToFilePaths.Where(keyValue => keyValue.Value.Count > 1)
                                                                         .ToDictionary(pair => pair.Key, pair => pair.Value);
            
            return duplicates;
        }

        private static void checkWithDatabase(Dictionary<string, List<string>> hashToFilePaths)
        {
            const string databaseFilePath = @"C:\Program Files\BackupService\Data\Data-7a160d7a-ceae-4df1-a0db-3a97cd2f1aec.json";
            string rawJson = File.ReadAllText(databaseFilePath);
            Dictionary<string, List<string>> alreadyBackedUp = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(rawJson)
                                                               ?? throw new NullReferenceException($"Failed to load '{databaseFilePath}'");

            foreach ((string hash, List<string> filePaths) in hashToFilePaths)
            {
                if (!alreadyBackedUp.TryGetValue(hash, out List<string>? backedUpFilePaths))
                {
                    continue;
                }
                
                filePaths.AddRange(backedUpFilePaths);
            }
        }

        private static void AddFileHashToGivenDict(IDictionary<string, List<string>> duplicatesFiles, string fileHash, string filePath)
        {
            if (duplicatesFiles.TryGetValue(fileHash, out List<string>? paths))
            {
                Console.WriteLine($"Hash {fileHash} found duplicate with file {filePath}");
                paths.Add(filePath);
            }
            else
            {
                duplicatesFiles.Add(fileHash, new List<string> { filePath });
            }
        }
    }
}