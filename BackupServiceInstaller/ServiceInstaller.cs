namespace BackupServiceInstaller;

public class ServiceInstaller
{
	private const string ServiceName = "Dor Backuper Service";
	private static readonly string mServicePath = @"C:/Program Files/BackupService/bin/BackupManager.exe";
	private readonly WindowsServiceManager mWindowsServiceManager;

	public ServiceInstaller(WindowsServiceManager windowsServiceManager)
	{
		mWindowsServiceManager = windowsServiceManager;
	}
	
	public void Install()
	{
		(bool isServiceExists, WindowsServiceHandle? serviceHandler) = mWindowsServiceManager.IsServiceExists(ServiceName); 
		if (isServiceExists && serviceHandler is not null)
		{
			_ = mWindowsServiceManager.StartService(serviceHandler, ServiceName);
			serviceHandler.Dispose();
			return;
		}
		
		using WindowsServiceHandle? serviceHandle = mWindowsServiceManager.TryCreateService(ServiceName, mServicePath);
		if (serviceHandle is null)
		{
			return;
		}
		
		_ = mWindowsServiceManager.StartService(serviceHandle, ServiceName);
	}
}