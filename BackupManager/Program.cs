using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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

        // TODO DOR initialize logger.
        // public static Program(ILogger logger)
        // {
        //     mLogger = logger ?? throw new ArgumentNullException($"{nameof(logger)} is null");
        // }

        private static async Task Main()
        {
            Console.WriteLine("Backuper is running!");

            if (!File.Exists(Consts.SettingsFilePath))
            {
                HandleMissingSettingsFile();
                return;
            }

            if (!ShouldStartBackupProcedure())
            {
                return;
            }

            await mBackuperService.BackupFiles(CancellationToken.None);
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