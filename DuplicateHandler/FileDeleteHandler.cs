namespace DuplicatesHandler;

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
		
		FileDeleter.DeleteFilesFrom(filePath);
	}
}