using System.Diagnostics;
using BackupManagerCore;
using Microsoft.Extensions.Logging;
using WindowsServiceHandle;

namespace BackupServiceInstaller;

public class ServiceInstaller
{
	private readonly WindowsServiceManager mWindowsServiceManager;
	private readonly ILogger<ServiceInstaller> mLogger;

	public ServiceInstaller(WindowsServiceManager windowsServiceManager, ILogger<ServiceInstaller> logger)
	{
		mWindowsServiceManager = windowsServiceManager;
		mLogger = logger;
	}
	
	public void Install(CancellationToken cancellationToken)
	{
		mLogger.LogInformation($"Checking if service {Consts.ServiceName} exists");
		(bool isServiceExists, WindowsServiceHandle.WindowsServiceHandle? serviceHandler) = mWindowsServiceManager.IsServiceExists(Consts.ServiceName); 
		if (isServiceExists && serviceHandler is not null)
		{
			mLogger.LogInformation($"Starting service {Consts.ServiceName}");
			_ = mWindowsServiceManager.StartService(serviceHandler, Consts.ServiceName);
			serviceHandler.Dispose();
		}
		else
		{
			mLogger.LogInformation($"Creating service {Consts.ServiceName}");
			using WindowsServiceHandle.WindowsServiceHandle? serviceHandle = mWindowsServiceManager.TryCreateService(Consts.ServiceName, Consts.ServicePath);
			if (serviceHandle is null)
			{
				return;
			}
		
			mLogger.LogInformation($"Starting service {Consts.ServiceName}");
			_ = mWindowsServiceManager.StartService(serviceHandle, Consts.ServiceName);
		}
		
		bool isCLICopied = CopyCLIToKnownLocation(cancellationToken);
		if (isCLICopied)
		{
			AddCLIAsEnvironmentVariable();
		}
	}

	private bool CopyCLIToKnownLocation(CancellationToken cancellationToken)
	{
		mLogger.LogInformation($"Copying CLI to {Consts.CliDirectoryPath}");
		string runningExeFilePath = Process.GetCurrentProcess().MainModule?.FileName!;
		string? cliSourceDirectory = Path.GetDirectoryName(runningExeFilePath);

		if (string.IsNullOrWhiteSpace(cliSourceDirectory))
		{
			mLogger.LogWarning("Failed to find CLI source directory");
			return false;
		}

		string cliSourceFilePath = Path.Combine(cliSourceDirectory, $"{nameof(BackupManagerCli)}.exe");
		if (!File.Exists(cliSourceFilePath))
		{
			mLogger.LogWarning($"Failed to find CLI source file at '{cliSourceFilePath}'");
			return false;
		}

		try
		{
			_ = Directory.CreateDirectory(Consts.CliDirectoryPath);
			IOWrapper.DirectoryOperations.CopyDirectory(cliSourceDirectory,
														Consts.CliDirectoryPath,
														"*",
														shouldOverwriteExist: true,
														SearchOption.AllDirectories,
														cancellationToken);
		
			string cliDestinationFilePath = Path.Combine(Consts.CliDirectoryPath, $"{nameof(BackupManagerCli)}.exe");
			File.Move(cliDestinationFilePath, Consts.CliFilePath, overwrite: true);
			return true;
		}
		catch (Exception ex)
		{
			mLogger.LogError(ex, "Failed to copy CLI to it's location");
			return false;
		}
	}

	private void AddCLIAsEnvironmentVariable()
	{
		EnvironmentVariableTarget environmentVariableTarget = EnvironmentVariableTarget.User;
		mLogger.LogInformation("Adding backup service CLI as environment variable");
	
		const string pathVariableName = "path";
		string? oldPathsEnvironmentVariableValue = Environment.GetEnvironmentVariable(pathVariableName, environmentVariableTarget);

		if (string.IsNullOrEmpty(oldPathsEnvironmentVariableValue))
		{
			Environment.SetEnvironmentVariable(pathVariableName, Consts.CliDirectoryPath, environmentVariableTarget);
			return;
		}

		HashSet<string> environmentVariablePaths = oldPathsEnvironmentVariableValue.Split(';').ToHashSet();

		if (environmentVariablePaths.Contains(Consts.CliDirectoryPath))
		{
			mLogger.LogInformation("Backup service CLI already added as environment variable");
			return;
		}

		string newPathEnvironmentVariableValue = oldPathsEnvironmentVariableValue + $";{Consts.CliDirectoryPath}";
		Environment.SetEnvironmentVariable(pathVariableName, newPathEnvironmentVariableValue, environmentVariableTarget);
		
		mLogger.LogInformation($"Now you can make backup service commands with {Consts.BackupServiceCliName}");
	}
}