using BackupManagerCore;
using IOWrapper;
using Newtonsoft.Json;

namespace BackupManagerCli;

public static class DatabaseMigrationHandler
{
	public static void Handle()
	{
		string fileBackupDataFilePath = Path.Combine(Consts.DataDirectoryPath, $"{Consts.BackupFilesCollectionName}.{Consts.LocalDatabaseExtension}");
		string filesBackupRawData = File.ReadAllText(fileBackupDataFilePath);
		Dictionary<string, List<string>> hashToFilesPathsMap = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(filesBackupRawData)
			?? throw new NullReferenceException($"Could not cast '{fileBackupDataFilePath}' into {nameof(Dictionary<string, List<string>>)}");

		foreach (List<string> filePaths in hashToFilesPathsMap.Values)
		{
			if (filePaths.Count != 1)
			{
				continue;
			}

			string filePath = filePaths[0];
			if (filePath.StartsWith("/WhatsApp/Media/WhatsApp Images/"))
			{
				FileSystemPath secondPathPrefix = new("/Android/media/com.whatsapp/");
				FileSystemPath secondFilePath = secondPathPrefix.Combine(filePath);
				filePaths.Add(secondFilePath.PathString);
			}
		}

		string newFilesBackupRawData = JsonConvert.SerializeObject(hashToFilesPathsMap);
		File.WriteAllText(fileBackupDataFilePath, newFilesBackupRawData);
	}
}