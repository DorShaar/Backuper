using BackuperApp;
using DuplicateChecker;
using System;
using System.Collections.Generic;

namespace BackuperManager
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Backuper is running!");

            Console.WriteLine("Choose one of the next options:");
            Console.WriteLine("1. Duplicate Check");
            Console.WriteLine("2. Backup");

            Checker checker = new Checker();
            string userInput = Console.ReadLine();
            while (userInput.ToLower() != "exit" || userInput.ToLower() != "q")
            {
                switch (userInput)
                {
                    case "1":
                        Dictionary<string, List<string>> hashToFilePathDict =
                            checker.GetDuplicateFiles("TODO get root dir from configuration file");
                        hashToFilePathDict.SaveToFile();
                        break;

                    case "2":
                        Backuper backuper = new Backuper();
                        DirectoriesBinding directoriesCouples = GetDirectoriesBinding();
                        DateTime lastUpdateTime = GetLastUpdateTime();
                        Dictionary<string, List<string>> hashToFilePathDict = GetHashToFilePathDict();
                        List<string> updatedFiles = backuper.BackupFiles(directoriesCouples, lastUpdateTime, hashToFilePathDict);
                        foreach (string updatedFile in updatedFiles)
                        {
                            checker.AddFileHash(updatedFile, hashToFilePathDict);
                        }

                        break;
                }
            }
        }
    }
}