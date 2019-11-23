using BackuperApp;
using DuplicateChecker;
using System;
using Microsoft.Extensions.DependencyInjection;
using FileHashes;
using Serializer.Interface;
using Microsoft.Extensions.Options;

namespace BackupManager
{
    class Program
    {
        private static readonly IServiceProvider mServiceProvider = new BackupManagerServiceProvider();

        static void Main()
        {
            Console.WriteLine("Backuper is running!");

            BackuperConfiguration config = mServiceProvider.GetRequiredService<IOptions<BackuperConfiguration>>().Value;
            IObjectSerializer objectSerializer = mServiceProvider.GetRequiredService<IObjectSerializer>();

            string userInput = string.Empty;
            while (userInput.ToLower() != "exit" && userInput.ToLower() != "q")
            {
                PrintMenu();
                userInput = Console.ReadLine();
                switch (userInput)
                {
                    case "1":
                        RunDuplicatedHashes(config, objectSerializer);
                        break;

                    case "2":
                        RunBackup(config, objectSerializer);
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

        private static void RunDuplicatedHashes(BackuperConfiguration config, IObjectSerializer objectSerializer)
        {
            Checker checker = new Checker();
            FilesHashesHandler filesHashesHandler = checker.GetDuplicateFiles(config.BackupRootDirectory);
            filesHashesHandler.Save(objectSerializer, config.FileHashesPath);
        }

        private static void RunBackup(BackuperConfiguration config, IObjectSerializer objectSerializer)
        {
            FilesHashesHandler filesHashesHandler = new FilesHashesHandler();
            filesHashesHandler.Load(objectSerializer, config.FileHashesPath);

            if (filesHashesHandler.Count == 0)
            {
                Console.WriteLine("Please run Duplicate Check or configure duplicates file");
                return;
            }

            Backuper backuper = new Backuper();
            backuper.BackupFiles(new DirectoriesBinding(config.DirectoriesCouples), config.LastUpdateTime, filesHashesHandler);
            filesHashesHandler.Save(objectSerializer, config.FileHashesPath);
        }
    }
}