namespace BackupManagerCli;

public static class FileDeleter
{
	public static void DeleteFilesFrom(string filePath)
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
			
			if (new FileInfo(fixedFilePathToDelete).Length == 0)
			{
				File.Delete(fixedFilePathToDelete);
				continue;
			}
			
			File.Delete(fixedFilePathToDelete);
		}
	}
}