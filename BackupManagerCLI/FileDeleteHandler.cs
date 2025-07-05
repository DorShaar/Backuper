namespace BackupManagerCli;

public static class FileDeleteHandler
{
	public static void Handle(string[] args)
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
		
		DeleteFilesFrom(filePath);
	}

    private static void DeleteFilesFrom(string filePath)
    {
        string[] filePathsToDelete = File.ReadAllLines(filePath);
        foreach (string filePathToDelete in filePathsToDelete)
        {
            string fixedFilePathToDelete = filePathToDelete.Trim();
            if (string.IsNullOrWhiteSpace(fixedFilePathToDelete))
            {
                continue;
            }

            if (!File.Exists(fixedFilePathToDelete))
            {
                continue;
            }

            File.Delete(fixedFilePathToDelete);
        }
    }
}