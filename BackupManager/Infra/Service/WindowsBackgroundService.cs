using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.App;
using BackupManager.Domain.Configuration;
using BackupManager.Domain.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OSOperations;

namespace BackupManager.Infra.Service;

public sealed class WindowsBackgroundService : BackgroundService
{
    private static readonly TimeSpan mDefaultCheckForBackupSettingsInterval = TimeSpan.FromMinutes(5);
    
    private readonly IBackupService mBackupService;
    private readonly BackupOptionsDetector mBackupOptionsDetector;
    private readonly IOptionsMonitor<BackupServiceConfiguration> mConfiguration;
    private readonly ILogger<WindowsBackgroundService> mLogger;

    public WindowsBackgroundService(IBackupService backupService,
        BackupOptionsDetector backupOptionsDetector,
        IOptionsMonitor<BackupServiceConfiguration> configuration,
        ILogger<WindowsBackgroundService> logger)
    {
        mBackupService = backupService;
        mBackupOptionsDetector = backupOptionsDetector;
        mConfiguration = configuration;
        mLogger = logger;

        if (mConfiguration.CurrentValue.CheckForBackupSettingsInterval == TimeSpan.Zero)
        {
            mLogger.LogInformation($"{nameof(mConfiguration.CurrentValue.CheckForBackupSettingsInterval)} was set to zero, setting to default {mDefaultCheckForBackupSettingsInterval}");
            mConfiguration.CurrentValue.CheckForBackupSettingsInterval = mDefaultCheckForBackupSettingsInterval;
        }
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        mLogger.LogInformation($"Starting Backup Service!");

        if (!Admin.IsRunningAsAdministrator())
        {
            string errorMessage = $"Backup Service must run under Administrator privileges";
            mLogger.LogCritical(errorMessage);
            throw new ApplicationException(errorMessage);
        }

        if (!File.Exists(Consts.SettingsFilePath))
        {
            HandleMissingSettingsFile();
        }

        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        mLogger.LogInformation($"Stopping Backup Service");
        
        return base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                List<BackupSettings>? backupOptionsList = mBackupOptionsDetector.DetectBackupOptions();

                try
                {
                    if (backupOptionsList is null || backupOptionsList.Count == 0)
                    {
                        mLogger.LogInformation($"No backup files settings found");
                        continue;
                    }
                
                    foreach (BackupSettings backupSettings in backupOptionsList)
                    {
                        mBackupService.BackupFiles(backupSettings, CancellationToken.None);
                    }
                }
                finally
                {
                    await Task.Delay(mConfiguration.CurrentValue.CheckForBackupSettingsInterval, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (TaskCanceledException)
        {
            mLogger.LogInformation($"Stopping backup service execution");
        }
        catch (Exception ex)
        {
            mLogger.LogError(ex, "{Message}", ex.Message);

            // Terminates this process and returns an exit code to the operating system.
            // This is required to avoid the 'BackgroundServiceExceptionBehavior', which
            // performs one of two scenarios:
            // 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
            // 2. When set to "StopHost": will cleanly stop the host, and log errors.
            //
            // In order for the Windows Service Management system to leverage configured
            // recovery options, we need to terminate the process with a non-zero exit code.
            Environment.Exit(1);
        }
    }
    
    
    // TODO DOR Service is doing a check every 10 minutes to see if it should operate.
    // TODO DOR maybe add an ability to detect if new device is added.
    // There are two kinds of operations:
    // 1. new device detected - start backup from that device if it is registered. The device may have different names every time,
    // so rely on device name is not smart. We should know if a device is backup-able is by a configuration file in it.
    // 2. Known backup directory has files in it, so we should start backup procedure to a device.
    
    private void HandleMissingSettingsFile()
    {
        string errorMessage = $"Configuration file {Consts.SettingsFilePath} does not exist, " +
                              $"Please copy example from {Consts.SettingsExampleFilePath} and place it in {Consts.SettingsFilePath}.";
        mLogger.LogCritical(errorMessage);
        createSettingsFileTemplate();
        throw new FileNotFoundException(errorMessage, Consts.SettingsFilePath);
    }

    private void createSettingsFileTemplate()
    {
        string executionDirectoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new NullReferenceException("No parent directory for execution assembly");
        string exampleConfigurationFle = Path.Combine(executionDirectoryPath, "Domain", "Configuration", "BackupConfig.json");
        File.Copy(exampleConfigurationFle, Consts.SettingsExampleFilePath);
    }
}