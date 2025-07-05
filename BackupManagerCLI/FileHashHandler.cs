using BackupManagerCore.Hash;
using JsonSerialization;

namespace BackupManagerCli;

public static class FileHashHandler
{
    private static readonly IJsonSerializer _jsonSerializer = new JsonSerializer();

    public static async Task MapFiles(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Please provide directory to map file hashes result output path");
            return;
        }

        string directoryPath = args[0];
        if (!Directory.Exists(directoryPath))
        {
            Console.WriteLine($"Directory '{directoryPath}' does not exist");
            return;
        }

        string resultOutputPath = args[1];

        Dictionary<string, List<string>> hashToFilePaths = GetHashToFilePathMap(directoryPath);
        await _jsonSerializer.SerializeAsync(hashToFilePaths, resultOutputPath, CancellationToken.None).ConfigureAwait(false);
    }

    public static async Task FindAlreadyBackupedFiles(string[] args)
	{
		if (args.Length < 3)
		{
			Console.WriteLine("Please provide directory to find duplicates, database path and result output path");
			return;
		}
		
		string directoryPath = args[0];
		if (!Directory.Exists(directoryPath))
		{
			Console.WriteLine($"Directory '{directoryPath}' does not exist");
			return;
		}

        string databasePath = args[1];
        if (!File.Exists(databasePath))
        {
            Console.WriteLine($"Database file path '{databasePath}' does not exist");
            return;
        }

        string resultOutputPath = args[2];

		List<string> backupedFiles = await FindAlreadyBackupedFilesInternal(directoryPath, databasePath).ConfigureAwait(false);
        await _jsonSerializer.SerializeAsync(backupedFiles, resultOutputPath, CancellationToken.None).ConfigureAwait(false);
	}

    public static async Task FindNonBackupedFiles(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Please provide directory to find non backup-ed files, database path and result output path");
            return;
        }

        string directoryPath = args[0];
        if (!Directory.Exists(directoryPath))
        {
            Console.WriteLine($"Directory '{directoryPath}' does not exist");
            return;
        }

        string databasePath = args[1];
        if (!File.Exists(databasePath))
        {
            Console.WriteLine($"Database file path '{databasePath}' does not exist");
            return;
        }

        string resultOutputPath = args[2];

        await FindNonBackupedFilesInternal(directoryPath, databasePath, resultOutputPath).ConfigureAwait(false);
    }

    private static Dictionary<string, List<string>> GetHashToFilePathMap(string rootDirectory)
    {
        Dictionary<string, List<string>> hashToFilePaths = [];

        Console.WriteLine($"Start iterative traversal from '{rootDirectory}'");

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

        Console.WriteLine($"Finished iterative traversal from '{rootDirectory}'");

        return hashToFilePaths;
    }

    private static async Task<List<string>> FindAlreadyBackupedFilesInternal(string rootDirectory, string databaseFilePath)
    {
        Dictionary<string, List<string>> hashToFilePaths = GetHashToFilePathMap(rootDirectory);

        await RemoveNonBackupedFilesFromMap(hashToFilePaths, databaseFilePath).ConfigureAwait(false);

        return hashToFilePaths.Values.SelectMany(list => list).ToList();
    }

    private static async Task CheckWithDatabase(Dictionary<string, List<string>> hashToFilePathMap, string databaseFilePath)
    {
        Dictionary<string, List<string>> alreadyBackedUp = 
            await _jsonSerializer.DeserializeAsync<Dictionary<string, List<string>>>(databaseFilePath, CancellationToken.None)
            .ConfigureAwait(false);

        foreach ((string hash, List<string> filePaths) in hashToFilePathMap)
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

    private static async Task FindNonBackupedFilesInternal(string directoryPath, string databaseFilePath, string  resultOutputPath)
    {
        Dictionary<string, List<string>> hashToFilePaths = [];

        Console.WriteLine($"Start iterative operation for finding non backuped files from '{directoryPath}'");

        Queue<string> directoriesToSearch = new();
        directoriesToSearch.Enqueue(directoryPath);

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

        Console.WriteLine($"Finished iterative operation for finding duplicate files from '{directoryPath}'");

        await RemoveAlreadyBackupedFilesFromMap(hashToFilePaths, databaseFilePath).ConfigureAwait(false);

        await _jsonSerializer.SerializeAsync(hashToFilePaths, resultOutputPath, CancellationToken.None)
            .ConfigureAwait(false);
    }

    private static async Task RemoveNonBackupedFilesFromMap(Dictionary<string, List<string>> hashToFilePaths, string databaseFilePath)
    {
        Dictionary<string, List<string>> alreadyBackedUp =
            await _jsonSerializer.DeserializeAsync<Dictionary<string, List<string>>>(databaseFilePath, CancellationToken.None)
            .ConfigureAwait(false);

        foreach (string hash in alreadyBackedUp.Keys)
        {
            if (hashToFilePaths.ContainsKey(hash))
            {
                continue;
            }

            hashToFilePaths.Remove(hash);
        }
    }

    private static async Task RemoveAlreadyBackupedFilesFromMap(Dictionary<string, List<string>> hashToFilePaths, string databaseFilePath)
    {
        Dictionary<string, List<string>> alreadyBackedUp =
            await _jsonSerializer.DeserializeAsync<Dictionary<string, List<string>>>(databaseFilePath, CancellationToken.None)
            .ConfigureAwait(false);

        foreach (string hash in alreadyBackedUp.Keys)
        {
            if (hashToFilePaths.ContainsKey(hash))
            {
                hashToFilePaths.Remove(hash);
            }
        }
    }
}