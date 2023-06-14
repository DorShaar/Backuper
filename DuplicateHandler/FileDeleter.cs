namespace DuplicatesHandler;

public class FileDeleter
{
	public void DeleteFilesFrom(string filePath)
	{
		string[] filePathsToDelete = File.ReadAllLines(filePath);
		foreach (string filePathToDelete in filePathsToDelete)
		{
			File.Delete(filePathToDelete);
		}
	}
}