namespace DuplicatesHandler;

public static class DuplicateCheckerHandler
{
	public static void Handle(string[] args)
	{
		if (args.Length < 2)
		{
			Console.WriteLine("Please provide directory to find duplicates and result output path");
		}
		
		string directoryPath = args[0];
		if (!Directory.Exists(directoryPath))
		{
			Console.WriteLine($"Directory '{directoryPath}' does not exist");
			return;
		}
		
		string resultOutputPath = args[1];

		DuplicateChecker duplicateChecker = new();
		Dictionary<string, List<string>> duplicates = duplicateChecker.FindDuplicateFiles(directoryPath);
		duplicateChecker.WriteDuplicateFiles(duplicates, resultOutputPath);		
	}
}