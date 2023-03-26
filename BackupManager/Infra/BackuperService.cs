using Backuper.Domain.Mapping;
using Backuper.App;
using System;
using System.Collections.Generic;
using System.IO;
using Backuper.Domain.Configuration;
using Microsoft.Extensions.Options;

namespace Backuper.Infra
{
    public class BackuperService : IBackuperService
    {
        private const string LastUpdateFileName = "BackupUpdatesTimeLog";
        
        // tODO DOR think about it.
        private const string BackupDirectoryName = "to_backup";

        private readonly FilesHashesHandler mFilesHashesHandler;
        private readonly string mBackupDriveDirectoryPath;
        private readonly string mLastUpdatedFilePath;
        private readonly DirectoriesMapping mDirectoriesMapping;

        public BackuperService(FilesHashesHandler filesHashesHandler, IOptions<BackuperConfiguration> configuration)
        {
            mFilesHashesHandler = filesHashesHandler ?? throw new ArgumentNullException(nameof(filesHashesHandler));
            string rootDirectory = configuration.Value.RootDirectory ?? throw new NullReferenceException(nameof(configuration.Value.RootDirectory));
            mLastUpdatedFilePath = Path.Combine(rootDirectory, LastUpdateFileName);
            
            if (configuration.Value.DirectoriesSourcesToDirectoriesDestinationMap is null)
            {
                throw new NullReferenceException($"{nameof(configuration.Value.DirectoriesSourcesToDirectoriesDestinationMap)} is null");
            }
            
            mDirectoriesMapping = new DirectoriesMapping(configuration.Value.DirectoriesSourcesToDirectoriesDestinationMap);

            // TODO DOR think after thinking on BackupDirectoryName.
            // string driveRootDirectory = Path.GetDirectoryName(mConfiguration.Value.RootDirectory)
            //                             ?? throw new NullReferenceException($"Directory of '{mConfiguration.Value.RootDirectory}' is empty");
            // mBackupDriveDirectoryPath = Path.Combine(driveRootDirectory, BackupDirectoryName);
        }

        // TODO DOR Add tests.
        public void BackupFiles()
        {
            if (!shouldBackup())
            {
                return;
            }
            
            foreach (DirectoriesMap directoriesMap in mDirectoriesMapping)
            {
                Console.WriteLine($"Heading to directory {directoriesMap.SourceDirectory}");
                Dictionary<string, string> filePathToFileHashMap = getFilesToBackup(Path.Combine(mBackupDriveDirectoryPath, directoriesMap.SourceDirectory));

                if (filePathToFileHashMap.Count == 0)
                {
                    Console.WriteLine($"No files to backup found in '{directoriesMap.SourceDirectory}'");
                    continue;
                }

                CopyBackupFiles(filePathToFileHashMap, directoriesMap);
            }

            mFilesHashesHandler.Save();
            UpdateLastBackupTime();
        }

        private bool shouldBackup()
        {
            DateTime lastBackupTime = GetLastBackupTime();
            if (lastBackupTime.Add(TimeSpan.FromDays(1)) > DateTime.Now)
            {
                Console.WriteLine($"Last backup performed less than a day ({lastBackupTime}), should not backup");
                return false;
            }
            
            Console.WriteLine($"Last backup performed on {lastBackupTime}, should backup");
            return true;
        }

        private DateTime GetLastBackupTime()
        {
            string[] allLines = File.ReadAllLines(mLastUpdatedFilePath);
            
            // Gets the last line in file.
            string lastUpdateTime = allLines[^1];
            return DateTime.Parse(lastUpdateTime);
        }

        private void UpdateLastBackupTime()
        {
            File.AppendAllText(mLastUpdatedFilePath, Environment.NewLine + DateTime.Now);
        }

        // TODO DOR replace all console writeline with logger to file and console.
        private Dictionary<string, string> getFilesToBackup(string sourceDirectory)
        {
            Dictionary<string, string> filePathToFileHashMap = new();

            if (!Directory.Exists(sourceDirectory))
            {
                Console.WriteLine($"'{sourceDirectory}' does not exists"); // TODO DOR Log error.
                return filePathToFileHashMap;
            }

            Console.WriteLine($"Start iterative operation for finding updated files from '{sourceDirectory}'");

            Queue<string> directoriesToSearch = new();
            directoriesToSearch.Enqueue(sourceDirectory);

            while (directoriesToSearch.Count > 0)
            {
                string currentSearchDirectory = directoriesToSearch.Dequeue();
                Console.WriteLine($"Collecting files from directory '{currentSearchDirectory}'");

                // Adding subdirectories to search.
                foreach (string directory in Directory.EnumerateDirectories(currentSearchDirectory))
                {
                    directoriesToSearch.Enqueue(directory);
                }

                AddFilesToBackup(filePathToFileHashMap, currentSearchDirectory);
            }

            Console.WriteLine($"Finished iterative operation for finding updated files from '{sourceDirectory}'");
            return filePathToFileHashMap;
        }

        private void AddFilesToBackup(IDictionary<string, string> filePathToFileHashMap, string directory)
        {
            foreach (string filePath in Directory.EnumerateFiles(directory))
            {
                (string fileHash, bool isFileHashExist) = mFilesHashesHandler.IsFileHashExist(filePath);  
                if (isFileHashExist)
                {
                    continue;
                }
                
                filePathToFileHashMap.Add(filePath, fileHash);
                Console.WriteLine($"Found file '{filePath}' to backup");
            }
        }

        private void CopyBackupFiles(Dictionary<string, string> filePathToBackupToFileHashMap, DirectoriesMap directoriesMap)
        {
            Console.WriteLine($"Copying {filePathToBackupToFileHashMap.Count} files from '{directoriesMap.SourceDirectory}' to '{directoriesMap.DestDirectory}'");
            foreach ((string fileToBackup, string fileHash) in filePathToBackupToFileHashMap)
            {
                string outputFile = directoriesMap.GetNewFilePath(fileToBackup);
                // TODO DOR use mConfiguration.Value.DriveRootDirectory as member.
                
                // TODO DOR validate logic.
                outputFile = outputFile.Replace(mBackupDriveDirectoryPath, mConfiguration.Value.RootDirectory);
                string outputDirectory = Path.GetDirectoryName(outputFile) ?? throw new NullReferenceException($"Directory of '{outputFile}' is empty"); 
                Directory.CreateDirectory(outputDirectory);

                try
                {
                    File.Copy(fileToBackup, outputFile, overwrite: true);
                    Console.WriteLine($"Copied {fileToBackup} to {outputFile}");
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"Failed to copy {fileToBackup} to {outputFile}. Exception: {ex.Message}"); // TODO DOR log error.
                }

                mFilesHashesHandler.AddFileHash(fileHash, outputFile);
                Console.WriteLine();
            }
        }
    }
}