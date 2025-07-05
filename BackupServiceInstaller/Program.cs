using JsonSerialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WindowsServiceHandle;

namespace BackupServiceInstaller;

public class Program
{
	public static async Task Main(string[] args)
	{
		using IHost host = Host.CreateDefaultBuilder(args)
							   .ConfigureServices(services =>
							   {
								   services.AddSingleton<ServiceInstaller>();
								   services.AddSingleton<WindowsServiceManager>();
                                   services.AddSingleton<IJsonSerializer, JsonSerializer>();
                                   services.AddLogging(loggerBuilder =>
								   {
									   loggerBuilder.ClearProviders();
									   loggerBuilder.AddConsole();
								   });
							   })
							   .Build();

		ServiceInstaller serviceInstaller = host.Services.GetRequiredService<ServiceInstaller>();
		await serviceInstaller.Install(CancellationToken.None).ConfigureAwait(false);
	}
}