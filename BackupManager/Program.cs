using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Backuper.Infra;
using Backuper.Domain.Configuration;
using Backuper.App.Serialization;
using Backuper.App;
using Backuper.Domain.Mapping;

namespace Backuper
{
    class Program
    {
        private static readonly IServiceProvider mServiceProvider = new BackupManagerServiceProvider();
        private static readonly BackuperConfiguration mConfig = mServiceProvider.GetRequiredService<IOptions<BackuperConfiguration>>().Value;
        private static readonly IObjectSerializer mObjectSerializer = mServiceProvider.GetRequiredService<IObjectSerializer>();
        private static readonly IBackuperService mBackuperService = mServiceProvider.GetRequiredService<IBackuperService>();
        private static readonly IDuplicateChecker mDuplicateChecker = mServiceProvider.GetRequiredService<IDuplicateChecker>();

        static void Main()
        {
            Console.WriteLine("Backuper is running!");

            string userInput = string.Empty;
            while (userInput.ToLower() != "exit" && userInput.ToLower() != "q")
            {
                PrintMenu();
                userInput = Console.ReadLine();
                switch (userInput)
                {
                    case "1":
                        FindDuplicatedHashes();
                        break;

                    case "2":
                        RunBackup();
                        break;
                }
            }
        }

        private static void PrintMenu()
        {
            Console.WriteLine();
            Console.WriteLine("Choose one of the next options:");
            Console.WriteLine("1. Duplicate Check");
            Console.WriteLine("2. Backup");
        }

        private static void FindDuplicatedHashes()
        {
            FilesHashesHandler filesHashesHandler = new FilesHashesHandler(mDuplicateChecker, mObjectSerializer);
            filesHashesHandler.FindDuplicatedHashes(mConfig.BackupRootDirectory);
            filesHashesHandler.Save(mConfig.FileHashesPath);
        }

        private static void RunBackup()
        {
            FilesHashesHandler filesHashesHandler = new FilesHashesHandler(mDuplicateChecker, mObjectSerializer);
            filesHashesHandler.Load(mConfig.FileHashesPath);

            if (filesHashesHandler.HashesCount == 0)
            {
                Console.WriteLine("No hashes file provided. Please run Duplicate Check or configure duplicates file");
                return;
            }

            mBackuperService.BackupFiles(new DirectoriesMapping(mConfig.DirectoriesCouples), mConfig.LastUpdateTime, filesHashesHandler);
            filesHashesHandler.Save(mConfig.FileHashesPath);
        }
    }
}