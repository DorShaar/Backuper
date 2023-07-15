namespace BackupManagerCli;

public static class Program
{
	private const string StartCommand = "start";
	private const string StopCommand = "stop";
	private const string GetStatusCommand = "status";
	private const string CreateSettingsCommand = "create-settings";
	private const string FindDuplicatesCommand = "finddup";
	private const string DeleteCommand = "delete";
	private const string ShowLogsCommand = "logs";
	
	private static readonly string[] mAllowedCommands =
	{
		StartCommand,
		StopCommand,
		GetStatusCommand,
		CreateSettingsCommand,
		FindDuplicatesCommand,
		DeleteCommand,
		ShowLogsCommand
	};

	public static async Task Main(string[] args)
	{
		if (args.Length < 1)
		{
			Console.WriteLine($"Please provide one of the next commands {string.Join(", ", mAllowedCommands)}");
			return;
		}
		
		string command = args[0].ToLower();
		switch (command)
		{
			case StartCommand:
				BackupServiceHandler.Start();
				break;
				
			case StopCommand:
				BackupServiceHandler.Stop();
				break;
			
			case GetStatusCommand:
				BackupServiceHandler.GetStatus();
				break;
			
			case CreateSettingsCommand:
				await CreateSettingsHandler.Handle(CancellationToken.None).ConfigureAwait(false);
				break;
				
			case FindDuplicatesCommand:
				DuplicateCheckerHandler.Handle(args[1..]);
				break;
					
			case DeleteCommand:
				FileDeleteHandler.Handle(args[1..]);
				break;

			case ShowLogsCommand:
				OpenLogsHandler.Handle();
				break;
			
			default:
				Console.WriteLine($"Command '{command}' is not valid");
				break;
		}
	}
}