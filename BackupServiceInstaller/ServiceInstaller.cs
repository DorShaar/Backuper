using System.Diagnostics;
using System.Threading;
using BackupManagerCore;
using BackupManagerCore.Settings;
using JsonSerialization;
using Microsoft.Extensions.Logging;
using WindowsServiceHandle;

namespace BackupServiceInstaller;

public class ServiceInstaller(WindowsServiceManager windowsServiceManager,
    IJsonSerializer jsonSerializer,
    ILogger<ServiceInstaller> logger)
{
	private readonly WindowsServiceManager _windowsServiceManager = windowsServiceManager;
	private readonly IJsonSerializer _jsonSerializer = jsonSerializer;
	private readonly ILogger<ServiceInstaller> _logger = logger;

    public async Task Install(CancellationToken cancellationToken)
	{
		_logger.LogInformation($"Checking if service {Consts.ServiceAndCLI.ServiceName} exists");
		(bool isServiceExists, WindowsServiceHandle.WindowsServiceHandle? serviceHandler) = _windowsServiceManager.IsServiceExists(Consts.ServiceAndCLI.ServiceName); 
		if (isServiceExists && serviceHandler is not null)
		{
			_logger.LogInformation($"Starting service {Consts.ServiceAndCLI.ServiceName}");
			_windowsServiceManager.StartService(serviceHandler, Consts.ServiceAndCLI.ServiceName);
			serviceHandler.Dispose();
			return;
		}

		_logger.LogInformation($"Creating service {Consts.ServiceAndCLI.ServiceName}");
		using WindowsServiceHandle.WindowsServiceHandle? serviceHandle = _windowsServiceManager.TryCreateService(Consts.ServiceAndCLI.ServiceName, Consts.ServiceAndCLI.ServicePath);
		if (serviceHandle is null)
		{
			return;
		}
		
		bool areBinariesCopied = CopyServiceAndCLIToKnownLocation(cancellationToken);
		if (!areBinariesCopied)
		{
			return;
		}

		AddCLIAsEnvironmentVariable();

		await CreateNonInitializedBackupSettings(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation($"Starting service {Consts.ServiceAndCLI.ServiceName}");
        _windowsServiceManager.StartService(serviceHandle, Consts.ServiceAndCLI.ServiceName);
    }

	private bool CopyServiceAndCLIToKnownLocation(CancellationToken cancellationToken)
	{
		_logger.LogInformation($"Copying Service and CLI to {Consts.ServiceAndCLI.ServiceDirectoryPath}");
		string runningExeFilePath = Process.GetCurrentProcess().MainModule?.FileName!;
		string? cliSourceDirectory = Path.GetDirectoryName(runningExeFilePath);

		if (string.IsNullOrWhiteSpace(cliSourceDirectory))
		{
			_logger.LogWarning("Failed to find CLI source directory");
			return false;
		}

		string cliSourceFilePath = Path.Combine(cliSourceDirectory, $"{nameof(BackupManagerCli)}.exe");
		if (!File.Exists(cliSourceFilePath))
		{
			_logger.LogWarning($"Failed to find CLI source file at '{cliSourceFilePath}'");
			return false;
		}

		try
		{
			_ = Directory.CreateDirectory(Consts.ServiceAndCLI.ServiceDirectoryPath);
			IOWrapper.DirectoryOperations.CopyDirectory(cliSourceDirectory,
														Consts.ServiceAndCLI.ServiceDirectoryPath,
														"*",
														shouldOverwriteExist: true,
														SearchOption.AllDirectories,
														cancellationToken);
		
			string cliDestinationFilePath = Path.Combine(Consts.ServiceAndCLI.ServiceDirectoryPath, $"{nameof(BackupManagerCli)}.exe");
			File.Move(cliDestinationFilePath, Consts.ServiceAndCLI.CliFilePath, overwrite: true);
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to copy CLI to it's location");
			return false;
		}
	}

    private void AddCLIAsEnvironmentVariable()
	{
		EnvironmentVariableTarget environmentVariableTarget = EnvironmentVariableTarget.User;
		_logger.LogInformation("Adding backup service CLI as environment variable");
	
		const string pathVariableName = "path";
		string? oldPathsEnvironmentVariableValue = Environment.GetEnvironmentVariable(pathVariableName, environmentVariableTarget);

		if (string.IsNullOrEmpty(oldPathsEnvironmentVariableValue))
		{
			Environment.SetEnvironmentVariable(pathVariableName, Consts.ServiceAndCLI.ServiceDirectoryPath, environmentVariableTarget);
			return;
		}

		HashSet<string> environmentVariablePaths = oldPathsEnvironmentVariableValue.Split(';').ToHashSet();

		if (environmentVariablePaths.Contains(Consts.ServiceAndCLI.ServiceDirectoryPath))
		{
			_logger.LogInformation("Backup service CLI already added as environment variable");
			return;
		}

		string newPathEnvironmentVariableValue = oldPathsEnvironmentVariableValue + $";{Consts.ServiceAndCLI.ServiceDirectoryPath}";
		Environment.SetEnvironmentVariable(pathVariableName, newPathEnvironmentVariableValue, environmentVariableTarget);
		
		_logger.LogInformation($"Now you can make backup service commands with {Consts.ServiceAndCLI.ServiceDirectoryPath}");
	}

	private async Task CreateNonInitializedBackupSettings(CancellationToken cancellationToken)
	{
		BackupSerializedSettings backupSerializedSettings = new()
		{
			IsFromInstallation = true,
            DirectoriesSourcesToDirectoriesDestinationMap = []
		};

		Directory.CreateDirectory(Consts.SettingsDirectoryPath);

		await _jsonSerializer.SerializeAsync(backupSerializedSettings, Consts.SettingsFilePath, cancellationToken)
			.ConfigureAwait(false);
    }
}