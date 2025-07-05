using JsonSerialization;

namespace BackupManagerCli;

public static class FileDeleteHandler
{
    private static readonly IJsonSerializer _jsonSerializer = new JsonSerializer();

    public static async Task Handle(string[] args)
	{
		if (args.Length < 1)
		{
			Console.WriteLine("Please provide file with paths to delete");
		}
		
		string filePath = args[0];
		if (!File.Exists(filePath))
		{
			Console.WriteLine($"File '{filePath}' does not exist");
			return;
		}
		
		await DeleteFilesFrom(filePath).ConfigureAwait(false);
	}

    private static async Task DeleteFilesFrom(string filePath)
    {
        List<string> filesToDelete = await _jsonSerializer.DeserializeAsync<List<string>>(filePath, CancellationToken.None)
            .ConfigureAwait(false);

        foreach (string filePathToDelete in filesToDelete)
        {
            string fixedFilePathToDelete = filePathToDelete.Trim();
            if (!File.Exists(fixedFilePathToDelete))
            {
                continue;
            }

            Console.WriteLine($"Deleting file '{fixedFilePathToDelete}'");
            File.Delete(fixedFilePathToDelete);
        }
    }
}