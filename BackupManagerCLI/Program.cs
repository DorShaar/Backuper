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
	private const string ShowLogsCommand2 = "log";
	private const string OpenDataDirectory = "data";
	private const string DatabaseMigration = "migration";
	private const string GetFileSrtucture = "get-files-structure";
	
	private static readonly string[] mAllowedCommands =
	{
		StartCommand,
		StopCommand,
		GetStatusCommand,
		CreateSettingsCommand,
		FindDuplicatesCommand,
		DeleteCommand,
		ShowLogsCommand,
		ShowLogsCommand2,
		OpenDataDirectory,
		DatabaseMigration,
        GetFileSrtucture,
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
			case ShowLogsCommand2:
				OpenLogsHandler.Handle();
				break;
			
			case OpenDataDirectory:
				OpenDataDirectoryHandler.Handle();
				break;
			
			case DatabaseMigration:
				DatabaseMigrationHandler.Handle();
				break;

			case GetFileSrtucture:
				await FileStuctureHandler.Handle(args[1..]);
				break;


            default:
				Console.WriteLine($"Command '{command}' is not valid");
				break;
		}
	}
}