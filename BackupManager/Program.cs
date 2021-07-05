using System;
using Backuper.Infra;
using Backuper.Domain.Configuration;
using Backuper.App.Serialization;
using Backuper.App;
using Backuper.Domain.Mapping;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using BackupManager.Infra;

namespace Backuper
{
    internal static class Program
    {
        private static readonly IServiceProvider mServiceProvider = new BackupManagerServiceProvider();
        private static readonly IOptions<BackuperConfiguration> mConfig = mServiceProvider.GetRequiredService<IOptions<BackuperConfiguration>>();
        private static readonly IObjectSerializer mObjectSerializer = mServiceProvider.GetRequiredService<IObjectSerializer>();
        private static readonly IBackuperService mBackuperService = mServiceProvider.GetRequiredService<IBackuperService>();
        private static readonly IDuplicateChecker mDuplicateChecker = mServiceProvider.GetRequiredService<IDuplicateChecker>();
        private static readonly UnregisteredHashesAdder mUnregisteredHashesAdder = mServiceProvider.GetRequiredService<UnregisteredHashesAdder>();
        private static readonly FilesHashesHandler mFilesHashesHandler = new FilesHashesHandler(mDuplicateChecker, mObjectSerializer, mUnregisteredHashesAdder, mConfig);

        private static void Main()
        {
            Console.WriteLine("Backuper is running!");

            string userInput = string.Empty;
            while (!string.Equals(userInput, "exit", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(userInput, "q", StringComparison.OrdinalIgnoreCase))
            {
                PrintMenu();
                userInput = Console.ReadLine();
                switch (userInput)
                {
                    case "1":
                        RunBackup();
                        break;

                    case "2":
                        SynchronizeHashes();
                        break;

                    case "3":
                        FindDuplicatedHashes();
                        break;
                }
            }
        }

        private static void PrintMenu()
        {
            Console.WriteLine();
            Console.WriteLine("Choose one of the next options:");
            Console.WriteLine("1. Backup");
            Console.WriteLine("2. Syncronize hashes");
            Console.WriteLine("3. Duplicate Check");
            Console.WriteLine();
            Console.WriteLine("Press \"exit\" or 'q' to quit :");
            Console.Write("Your input: ");
        }

        private static void RunBackup()
        {
            if (mFilesHashesHandler.HashesCount == 0)
            {
                Console.WriteLine("No hashes file provided. Please run Duplicate Check or configure duplicates file");
                return;
            }

            mBackuperService.BackupFiles(new DirectoriesMapping(mConfig.Value.DirectoriesCouples),
                mConfig.Value.LastUpdateTime,
                mFilesHashesHandler);

            // TODO update / add last update time
            // Map from inside disk.

            mFilesHashesHandler.WriteHashesFiles();
        }

        private static void SynchronizeHashes()
        {
            mFilesHashesHandler.UpdateUnregisteredHashes();
            mFilesHashesHandler.WriteHashesFiles();
        }

        private static void FindDuplicatedHashes()
        {
            mFilesHashesHandler.UpdateDuplicatedHashes();
            mFilesHashesHandler.WriteHashesFiles();
        }
    }
}