namespace DuplicatesHandler;

public static class Program
{
	public static void Main(string[] args)
	{
		if (args.Length < 1)
		{
			Console.WriteLine("Please provide mode (finddup, delete)");
			return;
		}
		
		string mode = args[0];
		switch (mode)
		{
			case "finddup":
			case "dup":
			case "find":
				DuplicateCheckerHandler.Handle(args[1..]);
				break;
				
			case "delete":
			case "del":
				FileDeleteHandler.Handle(args[1..]);
				break;
				
			default:
				Console.WriteLine($"Mode '{mode}' is not valid");
				break;
		}
	}
}