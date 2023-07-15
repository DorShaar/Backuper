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
		
		bool isServiceStarted = mWindowsServiceManager.StartService(Consts.ServiceName);
		if (!isServiceStarted)
		{
			Console.WriteLine($"Service '{Consts.ServiceName}' not started");
			return;
		}
		
		Console.WriteLine($"Service '{Consts.ServiceName}' started");
	}
	
	public static void Stop()
	{
		if (!Admin.IsRunningAsAdministrator())
		{
			Console.WriteLine($"Service operation requires administrator privileges");
			return;
		}
		
		bool isServiceStopped = mWindowsServiceManager.StopService(Consts.ServiceName);
		if (!isServiceStopped)
		{
			Console.WriteLine($"Service '{Consts.ServiceName}' failed to be stopped");
			return;
		}
		
		Console.WriteLine($"Service '{Consts.ServiceName}' stopped");
	}

	public static void GetStatus()
	{
		if (!Admin.IsRunningAsAdministrator())
		{
			Console.WriteLine($"Service operation requires administrator privileges");
			return;
		}
		
		ServiceStatuses serviceStatus = mWindowsServiceManager.GetServiceStatus(Consts.ServiceName);
		Console.WriteLine($"Service '{Consts.ServiceName}' status: {serviceStatus}");
	}
}