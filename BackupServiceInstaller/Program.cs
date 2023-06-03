using System.ServiceProcess;

namespace BackupServiceInstaller;

public class Program
{
	// TODO DOR add installer to create service.
	// http://www.tutorialspanel.com/how-to-start-stop-and-restart-windows-services-using-csharp/index.htm
	// https://learn.microsoft.com/en-us/dotnet/api/system.serviceprocess.servicecontroller?view=dotnet-plat-ext-7.0
	
	// https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service-with-installer
	public static void Main()
	{
		ServiceController serviceController = new ServiceController("");
		serviceController.WaitForStatus(ServiceControllerStatus.Paused);
		
		serviceController.
	}
}