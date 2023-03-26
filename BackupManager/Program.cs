using System;
using Backuper.Infra;
using Backuper.App;
using Microsoft.Extensions.DependencyInjection;

namespace Backuper
{
    internal static class Program
    {
        private static readonly IServiceProvider mServiceProvider = new BackupManagerServiceProvider();
        private static readonly IBackuperService mBackuperService = mServiceProvider.GetRequiredService<IBackuperService>();
        private static readonly FilesHashesHandler mFilesHashesHandler = mServiceProvider.GetRequiredService<FilesHashesHandler>();

        private static void Main()
        {
            Console.WriteLine("Backuper is running!");

            // TODO DOR look for BackuperConfig.json in known directories. If found, start compare...
            // TOdO DOR check if latest updated time is today. if not, start backup procedure.
            // TODO DOR After compare, copy to known dir location.
            // TODO DORAfter copy to known dir location,
            // TODO DOR check for not mapped file, add it with hash.
            
            string? userInput = string.Empty;
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
            Console.WriteLine("2. Synchronize hashes");
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

            mBackuperService.BackupFiles();
        }

        private static void SynchronizeHashes()
        {
            // TODO DOR
            // mFilesHashesHandler.UpdateUnregisteredHashes();
            // mFilesHashesHandler.WriteHashesFiles();
        }

        private static void FindDuplicatedHashes()
        {
            // TODO DOR
            // mFilesHashesHandler.UpdateDuplicatedHashes();
            // mFilesHashesHandler.WriteHashesFiles();
        }
    }
}