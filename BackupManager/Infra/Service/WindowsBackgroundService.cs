using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.App.Backup;
using BackupManager.App.Backup.Detectors;
using BackupManager.App.Backup.Services;
using BackupManager.App.Database.Sync;
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
    
    private readonly IBackupServiceFactory mBackupServiceFactory;
    private readonly IBackupSettingsDetector mBackupSettingsDetector;
    private readonly IDatabasesSynchronizer mDatabasesSynchronizer;
    private readonly IOptionsMonitor<BackupServiceConfiguration> mConfiguration;
    private readonly ILogger<WindowsBackgroundService> mLogger;

    public WindowsBackgroundService(IBackupServiceFactory backupServiceFactory,
                                    IBackupSettingsDetector backupSettingsDetector,
                                    IDatabasesSynchronizer databasesSynchronizer,
                                    IOptionsMonitor<BackupServiceConfiguration> configuration,
                                    ILogger<WindowsBackgroundService> logger)
    {
        mBackupServiceFactory = backupServiceFactory ?? throw new ArgumentException($"{nameof(backupServiceFactory)} is null");
        mBackupSettingsDetector = backupSettingsDetector ?? throw new ArgumentException($"{nameof(backupSettingsDetector)} is null");
        mDatabasesSynchronizer = databasesSynchronizer ?? throw new ArgumentException($"{nameof(databasesSynchronizer)} is null");
        mConfiguration = configuration ?? throw new ArgumentException($"{nameof(configuration)} is null");
        mLogger = logger ?? throw new ArgumentException($"{nameof(logger)} is null");

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
            string errorMessage = "Backup Service must run under Administrator privileges";
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
            HashSet<string> knownTokens = await GetKnownTokens(cancellationToken).ConfigureAwait(false);
            
            await mDatabasesSynchronizer.SyncDatabases(knownTokens, cancellationToken).ConfigureAwait(false);
            
            while (!cancellationToken.IsCancellationRequested)
            {
                List<BackupSettings>? backupOptionsList = await mBackupSettingsDetector.DetectBackupSettings(cancellationToken).ConfigureAwait(false);

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

                        bool isVerified = VerifyBackupSettings(backupSettings, knownTokens);

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

    private static void CreateSettingsFileTemplate()
    {
        string executionDirectoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new NullReferenceException("No parent directory for execution assembly");
        string exampleConfigurationFle = Path.Combine(executionDirectoryPath, "Domain", "Configuration", "BackupConfig.json");
        File.Copy(exampleConfigurationFle, Consts.SettingsExampleFilePath);
    }

    private bool VerifyBackupSettings(BackupSettings backupSettings, IReadOnlySet<string> knownTokens)
    {
        if (backupSettings.ShouldBackupToKnownDirectory)
        {
            return true;
        }

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
        
        if (!File.Exists(Consts.KnownTokensFilePath))
        {
            return knownTokens;
        }
        
        await foreach (string token in File.ReadLinesAsync(Consts.KnownTokensFilePath, cancellationToken).ConfigureAwait(false))
        {
            knownTokens.Add(token);
        }

        return knownTokens;
    }
}