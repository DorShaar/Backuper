using IOWrapper;
using JsonSerialization;
using System.Text;

namespace BackupManagerCli;

public static class FilesTreeStuctureHandler
{
    private static readonly IJsonSerializer _jsonSerializer = new JsonSerializer();

    public static async Task WriteToDisk(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Please provide directory to get file tree and output file path");
        }

        string directory = args[0];
        if (!Directory.Exists(directory))
        {
            Console.WriteLine($"Directory '{directory}' does not exist");
            return;
        }

        string outputFilePath = args[1];

        HashSet<string> relativeFilePaths = GetRelativeFilePaths(directory);
        await _jsonSerializer.SerializeAsync(relativeFilePaths, outputFilePath, CancellationToken.None)
            .ConfigureAwait(false);
    }

    public static async Task Compare(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine($"Please provide two file-trees (output from {nameof(WriteToDisk)} method) to compare with and an output file");
        }

        string filePath1 = args[0];
        if (!File.Exists(filePath1))
        {
            Console.WriteLine($"File '{filePath1}' does not exist");
            return;
        }

        string filePath2 = args[1];
        if (!File.Exists(filePath2))
        {
            Console.WriteLine($"File '{filePath2}' does not exist");
            return;
        }

        string outputFilePath = args[2];
        await WriteComparisonResults(filePath1, filePath2, outputFilePath).ConfigureAwait(false);
    }

    private static HashSet<string> GetRelativeFilePaths(string basePath)
    {
        string[] allFiles = Directory.GetFiles(basePath, "*", SearchOption.AllDirectories);
        HashSet<string> relativePaths = [];

        foreach (string fullPath in allFiles)
        {
            FileSystemPath file = new(fullPath);
            FileSystemPath relativePath = file.GetRelativePath(basePath);
            relativePaths.Add(relativePath.PathString);
        }

        return relativePaths;
    }

    private static async Task WriteComparisonResults(string treeAFilePath, string treeBFilePath, string outputFilePath)
    {
        HashSet<string> filesInA = await _jsonSerializer.DeserializeAsync<HashSet<string>>(treeAFilePath, CancellationToken.None)
            .ConfigureAwait(false);

        HashSet<string> filesInB = await _jsonSerializer.DeserializeAsync<HashSet<string>>(treeBFilePath, CancellationToken.None)
            .ConfigureAwait(false);

        List<string> missingInB = filesInA.Except(filesInB).ToList();
        List<string> additionalInB = filesInB.Except(filesInA).ToList();

        StringBuilder output = new();
        output.AppendLine("=========");
        output.AppendLine("Files missing in B:");
        foreach (string file in missingInB)
        {
            output.AppendLine(file);
        }

        output.AppendLine("=========");
        output.AppendLine("Files additional in B:");
        foreach (string file in additionalInB)
        {
            output.AppendLine(file);
        }

        await File.WriteAllTextAsync(outputFilePath, output.ToString()).ConfigureAwait(false);
    }
}
