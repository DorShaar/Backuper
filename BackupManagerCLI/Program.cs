﻿namespace BackupManagerCli;

public static class Program
{
	private const string StartCommand = "start";
	private const string StopCommand = "stop";
	private const string GetStatusCommand = "status";
	private const string CreateSettingsCommand = "create-settings";
	private const string UpdateSettingsCommand = "update-settings";
    private const string MapFilesCommand = "map-files";
	private const string FindAlreadyBackupedFilesCommand = "find-backuped";
	private const string FindNonBackupedFilesCommand = "find-non-backuped"; 
	private const string DeleteCommand = "delete";
	private const string ShowLogsCommand = "logs";
	private const string ShowLogsCommand2 = "log";
	private const string OpenDataDirectoryCommand = "data";
	private const string DatabaseMigrationCommand = "migration";
	private const string GetFilesTreeCommand = "get-files-tree";
	private const string CompareFilesTreeCommand = "compare-files-tree";
	private const string AlignToRelativeFilePathCommand = "align-to-relative";
	private const string CopyFilesFromRelativeCommand = "copy-files";
	
	private static readonly string[] mAllowedCommands =
	{
		StartCommand,
		StopCommand,
		GetStatusCommand,
		CreateSettingsCommand,
        UpdateSettingsCommand,
        MapFilesCommand,
        FindAlreadyBackupedFilesCommand,
        FindNonBackupedFilesCommand,
		DeleteCommand,
		ShowLogsCommand,
		ShowLogsCommand2,
		OpenDataDirectoryCommand,
		DatabaseMigrationCommand,
        GetFilesTreeCommand,
        CompareFilesTreeCommand,
        AlignToRelativeFilePathCommand,
        CopyFilesFromRelativeCommand,
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
				await CreateSettingsHandler.Create(args[1..]).ConfigureAwait(false);
				break;

			case UpdateSettingsCommand:
				await UpdateSettingsHandler.Update(args[1..]).ConfigureAwait(false);
				break;

            case MapFilesCommand:
				await FileHashHandler.MapFiles(args[1..]).ConfigureAwait(false);
                break;

            case FindAlreadyBackupedFilesCommand:
				await FileHashHandler.FindAlreadyBackupedFiles(args[1..]).ConfigureAwait(false);
				break;

            case FindNonBackupedFilesCommand:
                await FileHashHandler.FindNonBackupedFiles(args[1..]).ConfigureAwait(false);
                break;

            case DeleteCommand:
				await FileDeleteHandler.Handle(args[1..]).ConfigureAwait(false);
				break;

			case ShowLogsCommand:
			case ShowLogsCommand2:
				OpenLogsHandler.Handle();
				break;
			
			case OpenDataDirectoryCommand:
				OpenDataDirectoryHandler.Handle();
				break;
			
			case DatabaseMigrationCommand:
				DatabaseMigrationHandler.Handle();
				break;

			case GetFilesTreeCommand:
				await FilesTreeStuctureHandler.WriteToDisk(args[1..]);
				break;

			case CompareFilesTreeCommand:
                await FilesTreeStuctureHandler.Compare(args[1..]);
                break;

			case AlignToRelativeFilePathCommand:
				await RelativeFilePathAligner.Align(args[1..]);
				break;

			case CopyFilesFromRelativeCommand:
				await FileCopyHandler.Handle(args[1..]);
				break;

            default:
				Console.WriteLine($"Command '{command}' is not valid");
				break;
		}
	}
}