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

            mBackuperService.BackupFiles();

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