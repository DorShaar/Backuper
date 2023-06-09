namespace BackupServiceInstaller;

public class Program
{
	private const string ServiceName = "Dor Backuper Service";
	private static readonly string mServicePath = @"C:\Users\DorShaar\Dor\Personal\Projects\Backuper\BackupManager\bin\Debug\net7.0\win-x64\publish\BackupManager.exe"; // TODO DOR now
	
	public static void Main()
	{
		WindowsServiceManager windowsServiceManager = new();
		(bool isServiceExists, WindowsServiceHandle? serviceHandler) = windowsServiceManager.IsServiceExists(ServiceName); 
		if (isServiceExists && serviceHandler is not null)
		{
			_ = WindowsServiceManager.StartService(serviceHandler);
			serviceHandler.Dispose();
			return;
		}
		
		using WindowsServiceHandle? serviceHandle = windowsServiceManager.TryCreateService(ServiceName, mServicePath);
		if (serviceHandle is null)
		{
			return;
		}
		
		_ = WindowsServiceManager.StartService(serviceHandle);
	}
}