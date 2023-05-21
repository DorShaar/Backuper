using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.App.Backup.Services;
using BackupManager.Domain.Configuration;
using BackupManager.Domain.Settings;
using BackupManager.Infra.Backup;
using BackupManager.Infra.Backup.Detectors;
using BackupManager.Infra.DB.Sync;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OSOperations;

namespace BackupManager.Infra.Service;

public sealed class WindowsBackgroundService : BackgroundService
{
    private static readonly TimeSpan mDefaultCheckForBackupSettingsInterval = TimeSpan.FromMinutes(5);
    
    private readonly BackupServiceFactory mBackupServiceFactory;
    private readonly BackupSettingsDetector mBackupSettingsDetector;
    private readonly DatabasesSynchronizer mDatabasesSynchronizer;
    private readonly IOptionsMonitor<BackupServiceConfiguration> mConfiguration;
    private readonly ILogger<WindowsBackgroundService> mLogger;

    public WindowsBackgroundService(BackupServiceFactory backupServiceFactory,
                                    BackupSettingsDetector backupSettingsDetector,
                                    DatabasesSynchronizer databasesSynchronizer,
                                    IOptionsMonitor<BackupServiceConfiguration> configuration,
                                    ILogger<WindowsBackgroundService> logger)
    {
        mBackupServiceFactory = backupServiceFactory;
        mBackupSettingsDetector = backupSettingsDetector;
        mDatabasesSynchronizer = databasesSynchronizer;
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
            await mDatabasesSynchronizer.SyncDatabases(cancellationToken).ConfigureAwait(false);
            
            while (!cancellationToken.IsCancellationRequested)
            {
                List<BackupSettings>? backupOptionsList = await mBackupSettingsDetector.DetectBackupSettings(cancellationToken).ConfigureAwait(false);

                // TODO DOR - test.
                // In case we are backuping to not known directory, we need to verify first with Id that it is
                // a recognized drive we copy files into (have list of allowed token).
                
                // TOdO DOR add test - scenario of upading a file with hash x, while hash y exists in drive. hash should be updated from y to x and files should be updated too.

                try
                {
                    if (backupOptionsList is null || backupOptionsList.Count == 0)
                    {
                        mLogger.LogInformation($"No backup files settings found");
                        continue;
                    }
                
                    foreach (BackupSettings backupSettings in backupOptionsList)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            mLogger.LogInformation($"Cancel requested");
                            break;
                        }

                        bool isVerified = await VerifyBackupSettings(backupSettings, cancellationToken).ConfigureAwait(false);

                        if (!isVerified)
                        {
                            continue;
                        }

                        IBackupService backupService = mBackupServiceFactory.Create(backupSettings);
                        try
                        {
                            await backupService.BackupFiles(backupSettings, cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            mLogger.LogError(ex, $"Backup for '{backupSettings.Description}' stopped due to error. Settings: {backupSettings}");
                        }
                        finally
                        {
                            if (backupService is IDisposable disposableService)
                            {
                                disposableService.Dispose();
                            }
                        }
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
            mLogger.LogError(ex, "Backup operation stopped due to error");

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
        CreateSettingsFileTemplate();
        throw new FileNotFoundException(errorMessage, Consts.SettingsFilePath);
    }

    private void CreateSettingsFileTemplate()
    {
        string executionDirectoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new NullReferenceException("No parent directory for execution assembly");
        string exampleConfigurationFle = Path.Combine(executionDirectoryPath, "Domain", "Configuration", "BackupConfig.json");
        File.Copy(exampleConfigurationFle, Consts.SettingsExampleFilePath);
    }

    private async Task<bool> VerifyBackupSettings(BackupSettings backupSettings, CancellationToken cancellationToken)
    {
        if (backupSettings.ShouldBackupToKnownDirectory)
        {
            return true;
        }

        HashSet<string> knownTokens = await GetKnownTokens(cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(backupSettings.Token))
        {
            mLogger.LogError($"Token must exist for {backupSettings.Description ?? backupSettings.SourceType.ToString()}");
            return false;
        }

        if (!knownTokens.Contains(backupSettings.Token))
        {
            mLogger.LogError($"Invalid token for {backupSettings.Description ?? backupSettings.SourceType.ToString()}");
            return false;
        }

        mLogger.LogDebug($"Token verified for {backupSettings.Description ?? backupSettings.SourceType.ToString()}");
        return true;
    }

    private static async Task<HashSet<string>> GetKnownTokens(CancellationToken cancellationToken)
    {
        HashSet<string> knownTokens = new();
        await foreach (string token in File.ReadLinesAsync(Consts.KnownTokensFilePath, cancellationToken).ConfigureAwait(false))
        {
            knownTokens.Add(token);
        }

        return knownTokens;
    }
}