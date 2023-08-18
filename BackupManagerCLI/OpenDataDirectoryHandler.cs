using System.Diagnostics;
using BackupManagerCore;

namespace BackupManagerCli;

public static class OpenDataDirectoryHandler
{
	public static void Handle()
	{
		ProcessStartInfo processStartInfo = new()
		{
			 FileName = $"\"{Consts.BackupsDirectoryPath}\"",
			UseShellExecute = true
		};
		
		Process.Start(processStartInfo);
	}
}