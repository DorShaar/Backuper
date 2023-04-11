using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using Backuper.Infra;
using Backuper.App;
using BackupManager.Infra;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Backuper
{
    internal static class Program
    {
        private static readonly IServiceProvider mServiceProvider = new BackupManagerServiceProvider();
        private static readonly IBackuperService mBackuperService = mServiceProvider.GetRequiredService<IBackuperService>();
        private static ILogger mLogger;

        // TODO DOR Service is doing a check every 10 minutes to see if it should operate.
        // TODO DOR maybe add an ability to detect if new device is added.
        // There are two kinds of operations:
        // 1. new device detected - start backup from that device if it is registered. The device may have different names every time,
        // so rely on device name is not smart. We should know if a device is backup-able is by a configuration file in it.
        // 2. Known backup directory has files in it, so we should start backup procedure to a device.
        
        // TODO DOR initialize logger.
        // public static Program(ILogger logger)
        // {
        //     mLogger = logger ?? throw new ArgumentNullException($"{nameof(logger)} is null");
        // }

        private static void Main()
        {
            Console.WriteLine("Backuper is running!");

            if (isRunningAsAdministrator())
            {
                mLogger.LogCritical($"Backuper service must run under Administrator privileges");
                return;
            }

            if (!File.Exists(Consts.SettingsFilePath))
            {
                HandleMissingSettingsFile();
                return;
            }

            if (!ShouldStartBackupProcedure())
            {
                return;
            }

            mBackuperService.BackupFiles(CancellationToken.None);
        }
        
        private static bool isRunningAsAdministrator()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
#pragma warning disable CA1416
                WindowsIdentity windowsIdentity = WindowsIdentity.GetCurrent();
                WindowsPrincipal windowsPrincipal = new(windowsIdentity);
                return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
#pragma warning restore CA1416                
            }

            return true;
        }
        
        private static void HandleMissingSettingsFile()
        {
            mLogger.LogCritical($"Configuration file {Consts.SettingsFilePath} does not exist, " +
                                $"Please copy example from {Consts.SettingsExampleFilePath} and place it in " +
                                $"{Consts.SettingsFilePath}.");
            createSettingsFileTemplateIfNotExist();
        }

        // TODO DOR test this is working.
        private static void createSettingsFileTemplateIfNotExist()
        {
            JObject settingsTemplate = new()
            {
                { "fileMapping", "TOdO DOR" }
            };

            using StreamWriter streamWriter = File.CreateText(Consts.SettingsExampleFilePath);
            JsonSerializer serializer = new();
            serializer.Serialize(streamWriter, settingsTemplate);
        }

        private static bool ShouldStartBackupProcedure()
        {
            DateTime lastBackupTime = GetLastBackupTime();

            if (lastBackupTime.AddDays(1) < DateTime.Now)
            {
                mLogger.LogInformation($"Should start backup procedure. Last backup time: {lastBackupTime}");
                return true;
            }
            
            mLogger.LogInformation($"Should not start backup procedure yet. Last backup time: {lastBackupTime}");
            return false;
        }

        private static DateTime GetLastBackupTime()
        {
             string[] allLines = File.ReadAllLines(Consts.BackupTimeDiaryFilePath);
        
             // Gets the last line in file.
             string lastUpdateTime = allLines[^1];
             return DateTime.Parse(lastUpdateTime);
        }
    }
}