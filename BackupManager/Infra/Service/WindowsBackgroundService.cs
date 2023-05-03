using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.App;
using BackupManager.Domain.Configuration;
using BackupManager.Domain.Settings;
using BackupManager.Infra.Backup;
using BackupManager.Infra.Backup.Detectors;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OSOperations;

namespace BackupManager.Infra.Service;

public sealed class WindowsBackgroundService : BackgroundService
{
    private static readonly TimeSpan mDefaultCheckForBackupSettingsInterval = TimeSpan.FromMinutes(5);
    
    private readonly BackupServiceFactory mBackupServiceFactory;
    private readonly BackupOptionsDetector mBackupOptionsDetector;
    private readonly IOptionsMonitor<BackupServiceConfiguration> mConfiguration;
    private readonly ILogger<WindowsBackgroundService> mLogger;

    public WindowsBackgroundService(BackupServiceFactory backupServiceFactory,
        BackupOptionsDetector backupOptionsDetector,
        IOptionsMonitor<BackupServiceConfiguration> configuration,
        ILogger<WindowsBackgroundService> logger)
    {
        mBackupServiceFactory = backupServiceFactory;
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
                        IBackupService backupService = mBackupServiceFactory.Create(backupSettings);
                        backupService.BackupFiles(backupSettings, cancellationToken);
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