using BackupManagerCore;
using Microsoft.Extensions.Logging.Abstractions;
using OSOperations;
using WindowsServiceHandle;
using WindowsServiceHandle.Enums;

namespace BackupManagerCli;

public static class BackupServiceHandler
{
	private static readonly WindowsServiceManager mWindowsServiceManager = new(NullLogger<WindowsServiceManager>.Instance);
	
	public static void Start()
	{
		if (!Admin.IsRunningAsAdministrator())
		{
			Console.WriteLine($"Service operation requires administrator privileges");
			return;
		}
		
		bool isServiceStarted = mWindowsServiceManager.StartService(Consts.ServiceAndCLI.ServiceName);
		if (!isServiceStarted)
		{
			Console.WriteLine($"Service '{Consts.ServiceAndCLI.ServiceName}' not started");
			return;
		}
		
		Console.WriteLine($"Service '{Consts.ServiceAndCLI.ServiceName}' started");
	}
	
	public static void Stop()
	{
		if (!Admin.IsRunningAsAdministrator())
		{
			Console.WriteLine($"Service operation requires admiServiceAndCLI.nistrator privileges");
			return;
		}
		
		bool isServiceStopped = mWindowsServiceManager.StopService(Consts.ServiceAndCLI.ServiceName);
		if (!isServiceStopped)
		{
			Console.WriteLine($"Service '{Consts.ServiceAndCLI.ServiceName}' failed to be stopped");
			return;
		}
		
		Console.WriteLine($"Service '{Consts.ServiceAndCLI.ServiceName}' stopped");
	}

	public static void GetStatus()
	{
		if (!Admin.IsRunningAsAdministrator())
		{
			Console.WriteLine($"Service operation requires administrator privileges");
			return;
		}
		
		ServiceStatuses serviceStatus = mWindowsServiceManager.GetServiceStatus(Consts.ServiceAndCLI.ServiceName);
		Console.WriteLine($"Service '{Consts.ServiceAndCLI.ServiceName}' status: {serviceStatus}");
	}
}