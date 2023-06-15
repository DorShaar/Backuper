namespace DuplicatesHandler
{
    public class DuplicateChecker
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
            Dictionary<string, List<string>> duplicatesFiles = new();

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
                    AddFileHashToGivenDict(duplicatesFiles, HashCalculator.CalculateHash(filePath), filePath);
                }
            }

            Console.WriteLine($"Finished iterative operation for finding duplicate files from {rootDirectory}");
            return duplicatesFiles;
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