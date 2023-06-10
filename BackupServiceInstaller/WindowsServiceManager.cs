using System.ComponentModel;
using System.Runtime.InteropServices;
using BackupServiceInstaller.Enums;
using Microsoft.Extensions.Logging;

namespace BackupServiceInstaller;

public class WindowsServiceManager
{
	#region Dll Imports
	[DllImport("advapi32.dll", SetLastError = true)]
	private static extern IntPtr OpenSCManager(string? machineName, string? databaseName, int accessRights);

	[DllImport("advapi32.dll", SetLastError = true)]
	private static extern IntPtr CreateService(IntPtr SC_HANDLE,
											   string lpSvcName,
											   string lpDisplayName,
											   int dwDesiredAccess,
											   int dwServiceType,
											   int dwStartType,
											   int dwErrorControl,
											   string lpPathName,
											   string? lpLoadOrderGroup,
											   IntPtr lpdwTagId, // Must be byValue and not by ref (according to https://www.pinvoke.net/default.aspx/advapi32.createservice)
											   string? lpDependencies,
											   string? lpServiceStartName,
											   string? lpPassword);

	[DllImport("advapi32.dll", SetLastError = true)]
	private static extern int StartService(IntPtr SVHANDLE, int dwNumServiceArgs, string[]? lpServiceArgVectors);
	
	[DllImport("advapi32.dll", SetLastError = true)]
	private static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, int dwDesiredAccess);
	#endregion Dll Imports

	private readonly ILogger<WindowsServiceManager> mLogger;

	public WindowsServiceManager(ILogger<WindowsServiceManager> logger)
	{
		mLogger = logger ?? throw new ArgumentNullException($"{nameof(logger)} is null");
	}
	
	public (bool, WindowsServiceHandle?) IsServiceExists(string serviceName)
	{
		const int serviceDoesNotExistsWin32ErrorCode = 1060;
	
		bool isServiceExists;

		WindowsServiceHandle? serviceHandle = null;
		try
		{
			using WindowsServiceHandle serviceControlManagerHandle = OpenServiceControlManager(WindowsServiceManagerAccessRights.Connect | WindowsServiceManagerAccessRights.CreateService);
			serviceHandle = openService(serviceControlManagerHandle, serviceName, WindowsServiceAccessRights.QueryStatus | WindowsServiceAccessRights.Start);
			isServiceExists = true;
			mLogger.LogInformation($"Service {serviceName} already exists");
		}
		catch (Win32Exception ex)
		{
			if (ex.NativeErrorCode != serviceDoesNotExistsWin32ErrorCode)
			{
				mLogger.LogInformation(ex, $"Failed to check if service {serviceName} exists. Error code: {ex.NativeErrorCode}");
				throw;
			}

			mLogger.LogInformation($"Service {serviceName} does not exist");
			isServiceExists = false;
		}

		return (isServiceExists, serviceHandle);
	}

	public bool StartService(WindowsServiceHandle serviceHandle, string serviceName)
	{
		const int serviceAlreadyRunningError = 1056;
		int startResult = StartService(serviceHandle.Handle,0,null);
		if (startResult == 0)
		{
			int errorCode = Marshal.GetLastWin32Error();
			if (errorCode == serviceAlreadyRunningError)
			{
				mLogger.LogInformation($"While trying to start, service {serviceName} already running");
				return true;
			}
			
			mLogger.LogError($"Failed to start service {serviceName}. Error code: {errorCode}");
			return false;
		}

		mLogger.LogInformation($"Service {serviceName} started");
		return true;
	}

	public WindowsServiceHandle? TryCreateService(string serviceName, string serviceExePath)
	{
		const int serviceWin32OwnProcess = 0x00000010;
		
		try
		{
			using WindowsServiceHandle serviceControlManagerHandle = OpenServiceControlManager(WindowsServiceManagerAccessRights.CreateService);
			IntPtr servicePtr = CreateService(serviceControlManagerHandle.Handle,
											  serviceName,
											  serviceName,
											  (int)(WindowsServiceAccessRights.Start | WindowsServiceAccessRights.Stop | WindowsServiceAccessRights.StandardRightsRequired),
											  serviceWin32OwnProcess,
											  (int)WindowsServiceStartOptions.DemandStart,
											  (int)WindowsServiceErrorOptions.Normal,
											  $"\"{serviceExePath}\"",
											  null,
											  IntPtr.Zero,
											  null,
											  null,
											  null);
			WindowsServiceHandle serviceHandle = new(servicePtr);
			mLogger.LogInformation($"Service {serviceName} created from path '{serviceExePath}'");
			return serviceHandle;
		}
		catch (Exception ex)
		{
			mLogger.LogError(ex, $"Could not create service {serviceName} from path '{serviceExePath}'");
			return null;
		}
	}
	
	private static WindowsServiceHandle OpenServiceControlManager(WindowsServiceManagerAccessRights accessRights)
	{
		IntPtr serviceControlManagerPtr = OpenSCManager(machineName: null, databaseName: null, (int)accessRights);
		return new WindowsServiceHandle(serviceControlManagerPtr);
	}
	
	private static WindowsServiceHandle openService(WindowsServiceHandle serviceManagerHandle, string serviceName, WindowsServiceAccessRights accessRights)
	{
		IntPtr servicePtr = OpenService(serviceManagerHandle.Handle, serviceName, (int)accessRights);
		return new WindowsServiceHandle(servicePtr);
	}
}