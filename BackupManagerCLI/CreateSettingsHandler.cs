using System.Globalization;
using BackupManagerCore;
using BackupManagerCore.Mapping;
using BackupManagerCore.Settings;
using JsonSerialization;

namespace BackupManagerCli;

public static class CreateSettingsHandler
{
	private const string StopCommand = "done";
	private static readonly IJsonSerializer mJsonSerializer = new JsonSerializer();
	
	public static async Task Handle(CancellationToken cancellationToken)
	{
		string settingsFilePath = Path.Combine(Consts.TempDirectoryPath, DateTime.Now.ToString("dd-MM-yy h:mm:ss", CultureInfo.InvariantCulture), Consts.SettingsFileName);
		
		BackupSerializedSettings backupSerializedSettings = new()
		{
			DirectoriesSourcesToDirectoriesDestinationMap = GetDirectoriesMapFromUser(),
			Description = GetDeviceDescription(),
			AllowMultithreading = GetAllowMultithreading(),
			RootDirectory = GetRootDirectory(),
			ShouldFastMapFiles = GetShouldFastMapFiles(),
			ShouldBackupToKnownDirectory = GetShouldBackupToKnownDirectory(),
		};
		
		await mJsonSerializer.SerializeAsync(backupSerializedSettings, settingsFilePath, cancellationToken).ConfigureAwait(false);

		Console.WriteLine($"Created settings file can be found here: '{settingsFilePath}'. Copy it to the relevant device");
	}

	private static List<DirectoriesMap> GetDirectoriesMapFromUser()
	{
		Console.WriteLine($"Type directories mapping. Type '{StopCommand}' when finish");

		List<DirectoriesMap> map = new();
		
		bool finishRequested = false;
		while (!finishRequested)
		{
			string sourceRelativeDirectory = GetSourceRelativeDirectory();

			if (sourceRelativeDirectory.Equals(StopCommand, StringComparison.OrdinalIgnoreCase))
			{
				if (map.Count == 0)
				{
					Console.WriteLine("Cannot stop when mapping is empty");
				}
				else
				{
					finishRequested = true;
				}
				
				continue;
			}

			string? destinationRelativeDirectory = GetDestinationRelativeDirectory();

			if (destinationRelativeDirectory?.Equals(StopCommand, StringComparison.OrdinalIgnoreCase) ?? false)
			{
				if (map.Count == 0)
				{
					Console.WriteLine("Cannot stop when mapping is empty");
				}
				else
				{
					finishRequested = true;
				}
				
				continue;
			}

			DirectoriesMap directoriesMap = new()
			{
				SourceRelativeDirectory = sourceRelativeDirectory,
				DestRelativeDirectory = destinationRelativeDirectory,
			};
			
			Console.WriteLine($"Adding map: {directoriesMap.SourceRelativeDirectory} => {directoriesMap.DestRelativeDirectory}");
			
			map.Add(directoriesMap);
		}

		return map;
	}

	private static string GetSourceRelativeDirectory()
	{
		while (true)
		{
			string? userInput = Console.ReadLine();

			if (string.IsNullOrWhiteSpace(userInput))
			{
				Console.WriteLine("Invalid empty user input");
				continue;
			}
			
			return userInput;
		}
	}
	
	private static string? GetDestinationRelativeDirectory()
	{
		return Console.ReadLine();
	}

	private static string? GetDeviceDescription()
	{
		Console.WriteLine("Type the description of the device. May be empty");

		return Console.ReadLine();
	}
	
	private static string? GetRootDirectory()
	{
		Console.WriteLine("Type the root directory of the device. May be empty");

		return Console.ReadLine();
	}

	private static bool GetAllowMultithreading()
	{
		Console.WriteLine("Do you wish to allow backup service to work with multithreading (Might not work on all devices)?");
		return GetValidBooleanInput();
	}

	private static bool GetShouldFastMapFiles()
	{
		Console.WriteLine("Do you wish to apply fast files mapping? Fast mapping means to compare files by paths and not by hash, which may be inaccurate. The recommendation is false");
		return GetValidBooleanInput();
	}

	private static bool GetShouldBackupToKnownDirectory()
	{
		/// If True, backups from a location in the root directory to the known backup directory.
		/// If False, backups from known backup directory to a location in the root directory.
		Console.WriteLine("Is the device should backup to your computer?");
		return GetValidBooleanInput();
	}

	private static bool GetValidBooleanInput()
	{
		while (true)
		{
			string? userInput = Console.ReadLine();
			
			if (string.IsNullOrWhiteSpace(userInput))
			{
				Console.WriteLine("Invalid empty user input, should be boolean");
				continue;
			}
		
			if (userInput.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
				userInput.Equals("y", StringComparison.OrdinalIgnoreCase) ||
				userInput.Equals("true", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			if (userInput.Equals("no", StringComparison.OrdinalIgnoreCase) ||
				userInput.Equals("n", StringComparison.OrdinalIgnoreCase) ||
				userInput.Equals("false", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
		
			Console.WriteLine("Invalid boolean user input");
		}
	}
}