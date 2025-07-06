using BackupManagerCore;
using BackupManagerCore.Mapping;
using BackupManagerCore.Settings;
using JsonSerialization;

namespace BackupManagerCli
{
    public static class UpdateSettingsHandler
    {
        private const string StopCommand = "done";
        private static readonly IJsonSerializer _jsonSerializer = new JsonSerializer();

        public static async Task Update(string[] args)
        {
            string settingsFilePath = args[0];

            if (!File.Exists(settingsFilePath))
            {
                Console.WriteLine($"Settings file '{settingsFilePath}' does not exist");
                return;
            }

            await UpdateDirectoriesMapping(settingsFilePath).ConfigureAwait(false);
        }

        private static async Task UpdateDirectoriesMapping(string settingsFilePath)
        {
            BackupSerializedSettings settings = 
                await _jsonSerializer.DeserializeAsync<BackupSerializedSettings>(settingsFilePath, CancellationToken.None)
                                     .ConfigureAwait(false);

            bool shouldStop = false;
            while (!shouldStop)
            {
                PrintOptions(settings.DirectoriesSourcesToDirectoriesDestinationMap);

                Console.WriteLine($"Select an option or type {StopCommand} to finish");
                string? input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("Invalid empty input");
                    continue;
                }

                if (input.Equals(StopCommand, StringComparison.OrdinalIgnoreCase))
                {
                    shouldStop = true;
                    continue;
                }

                if (!int.TryParse(input, out int choice))
                {
                    Console.WriteLine("Invalid input. Try a number.");
                    continue;
                }

                if (choice == 0)
                {
                    AddMapping(settings.DirectoriesSourcesToDirectoriesDestinationMap, settings.RootDirectory);
                }
                else if (choice > 0 && choice <= settings.DirectoriesSourcesToDirectoriesDestinationMap.Count)
                {
                    DeleteMapping(settings.DirectoriesSourcesToDirectoriesDestinationMap, choice - 1);
                }
                else
                {
                    Console.WriteLine("Invalid selection.");
                    continue;
                }

                await _jsonSerializer.SerializeAsync(settings, settingsFilePath, CancellationToken.None)
                                     .ConfigureAwait(false);
            }
        }

        private static void PrintOptions(List<DirectoriesMap> directoriesSourcesToDirectoriesDestinationMap)
        {
            Console.WriteLine("Backup Mapping directories");
            Console.WriteLine("0. Add new mapping");

            int i = 1;
            foreach (DirectoriesMap directoriesMap in directoriesSourcesToDirectoriesDestinationMap)
            {
                Console.WriteLine($"{i}. {directoriesMap.SourceRelativeDirectory} → {directoriesMap.DestRelativeDirectory}");
                i++;
            }
        }

        private static void AddMapping(List<DirectoriesMap> mappings, string? backupDriveRootDirectory)
        {
            Console.Write("Enter full path source directory (we will handler the relative path...): ");
            string source = Console.ReadLine()?.Trim().Trim('"') ?? string.Empty;

            if (!Directory.Exists(source))
            {
                Console.WriteLine($"Source directory '{source}' does not exist");
                return;
            }

            Console.Write("Enter destination directory: ");
            string destination = Console.ReadLine()?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(destination))
            {
                Console.WriteLine("Source and destination cannot be empty");
                return;
            }

            DirectoriesMap directoriesMap = new()
            {
                SourceRelativeDirectory = Path.GetRelativePath(Consts.Data.ReadyToBackupDirectoryPath, source),
                DestRelativeDirectory = Path.GetRelativePath(backupDriveRootDirectory ?? string.Empty, destination)
            };

            mappings.Add(directoriesMap);
            Console.WriteLine($"Added mapping: {directoriesMap.SourceRelativeDirectory} → {directoriesMap.DestRelativeDirectory}");
        }

        private static void DeleteMapping(List<DirectoriesMap> mappings, int indexToRemove)
        {
            DirectoriesMap removedMap = mappings[indexToRemove];
            Console.WriteLine($"Deleting mapping: {removedMap.SourceRelativeDirectory} → {removedMap.DestRelativeDirectory}");
            mappings.RemoveAt(indexToRemove);
        }
    }
}
