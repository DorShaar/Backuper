using System.Diagnostics;
using BackupManagerCore;

namespace BackupManagerCli;

public static class OpenLogsHandler
{
	public static void Handle()
	{
		string lastLogFile = Directory.GetFiles(Consts.LogsDirectoryPath)
									  .Select(file => new FileInfo(file))
									  .OrderBy(fileInfo => fileInfo.LastWriteTime)
									  .First()
									  .FullName;
		
		Process.Start("notepad++", $"\"{lastLogFile}\"");
	}
}