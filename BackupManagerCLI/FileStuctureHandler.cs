using IOWrapper;
using JsonSerialization;

namespace BackupManagerCli;

public static class FileStuctureHandler
{
    private static readonly IJsonSerializer _jsonSerializer = new JsonSerializer();

    public static async Task Handle(string[] args)
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
}
