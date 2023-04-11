using Backuper.Domain.Configuration;
using Backuper.Domain.Mapping;
using Backuper.App;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.Infra;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Backuper.Infra
{
    public class BackuperService : IBackuperService
    {
        private readonly FilesHashesHandler mFilesHashesHandler;
        private readonly bool mShouldBackupToSelf;
        private readonly string mRootBackupSourceDirectoryPath;
        private readonly DirectoriesMapping mDirectoriesMapping;
        private readonly ILogger<BackuperService> mLogger;

        public BackuperService(FilesHashesHandler filesHashesHandler,
            IOptions<BackuperConfiguration> configuration,
            ILogger<BackuperService> logger)
        {
            mFilesHashesHandler = filesHashesHandler ?? throw new ArgumentNullException(nameof(filesHashesHandler));
            mRootBackupSourceDirectoryPath = configuration.Value.RootDirectory ?? throw new ArgumentNullException(nameof(configuration.Value.RootDirectory));
            mShouldBackupToSelf = configuration.Value.ShouldBackupToSelf;
            
            if (configuration.Value.DirectoriesSourcesToDirectoriesDestinationMap is null)
            {
                throw new NullReferenceException($"{nameof(configuration.Value.DirectoriesSourcesToDirectoriesDestinationMap)} is null");
            }
            
            mDirectoriesMapping = new DirectoriesMapping(configuration.Value.DirectoriesSourcesToDirectoriesDestinationMap);
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // TODO DOR Add tests.
        public void BackupFiles(CancellationToken cancellationToken)
        {
            // TODO dor handle cases of mShouldBackupToSelf. 
            
            List<Task> backupTasks = new();
            
            foreach (DirectoriesMap directoriesMap in mDirectoriesMapping)
            {
                mLogger.LogInformation($"Handling backup from '{directoriesMap.SourceRelativeDirectory}' to '{directoriesMap.DestRelativeDirectory}'");

                string sourceDirectoryToBackup = Path.Combine(mRootBackupSourceDirectoryPath, directoriesMap.SourceRelativeDirectory);
                Dictionary<string, string> filePathToFileHashMap = getFilesToBackup(sourceDirectoryToBackup);

                if (filePathToFileHashMap.Count == 0)
                {
                    mLogger.LogInformation($"No files found to backup in '{directoriesMap.SourceRelativeDirectory}'");
                    continue;
                }

                Task backupTask = Task.Run(() => BackupFiles(filePathToFileHashMap, directoriesMap), cancellationToken);
                backupTasks.Add(backupTask);
            }

            Task.WaitAll(backupTasks.ToArray(), cancellationToken);

            // mFilesHashesHandler.Save(); // TOdo dor think about it
            UpdateLastBackupTime();
        }

        private static void UpdateLastBackupTime()
        {
            File.AppendAllText(Consts.BackupTimeDiaryFilePath, Environment.NewLine + DateTime.Now);
        }

        private Dictionary<string, string> getFilesToBackup(string directoryToBackup)
        {
            Dictionary<string, string> filePathToFileHashMap = new();

            if (!Directory.Exists(directoryToBackup))
            {
                mLogger.LogError($"'{directoryToBackup}' does not exists");
                return filePathToFileHashMap;
            }

            mLogger.LogInformation($"Starting iterative operation for finding files to backup from '{directoryToBackup}'");

            Queue<string> directoriesToSearch = new();
            directoriesToSearch.Enqueue(directoryToBackup);

            while (directoriesToSearch.Count > 0)
            {
                string currentSearchDirectory = directoriesToSearch.Dequeue();
                mLogger.LogDebug($"Collecting files from directory '{currentSearchDirectory}'");

                // Adding subdirectories to search.
                foreach (string directory in Directory.EnumerateDirectories(currentSearchDirectory))
                {
                    directoriesToSearch.Enqueue(directory);
                }

                AddFilesToBackupOnlyIfFileNotBackupedAlready(filePathToFileHashMap, currentSearchDirectory);
            }

            mLogger.LogInformation($"Finished iterative operation for finding updated files from '{directoryToBackup}'");
            return filePathToFileHashMap;
        }

        private void AddFilesToBackupOnlyIfFileNotBackupedAlready(IDictionary<string, string> filePathToFileHashMap, string directory)
        {
            foreach (string filePath in Directory.EnumerateFiles(directory))
            {
                (string fileHash, bool isFileHashExist) = mFilesHashesHandler.IsFileHashExist(filePath);
                if (isFileHashExist)
                {
                    continue;
                }
                
                filePathToFileHashMap.Add(filePath, fileHash);
                mLogger.LogInformation($"Found file '{filePath}' to backup");
            }
        }

        private void BackupFiles(Dictionary<string, string> filePathToBackupToFileHashMap, DirectoriesMap directoriesMap)
        {
            mLogger.LogInformation($"Copying {filePathToBackupToFileHashMap.Count} files from '{directoriesMap.SourceRelativeDirectory}' to '{directoriesMap.DestRelativeDirectory}'");
            foreach ((string fileToBackup, string fileHash) in filePathToBackupToFileHashMap)
            {
                string destinationFilePath = fileToBackup.Replace($"{Path.DirectorySeparatorChar}{directoriesMap.SourceRelativeDirectory}{Path.DirectorySeparatorChar}",
                $"{Path.DirectorySeparatorChar}{directoriesMap.DestRelativeDirectory}{Path.DirectorySeparatorChar}");

                if (mShouldBackupToSelf)
                {
                    // TODO DOR handle.
                }
                else
                {
                    destinationFilePath = destinationFilePath.Replace(mRootBackupSourceDirectoryPath, Consts.DataDirectoryPath);
                }
                
                string outputDirectory = Path.GetDirectoryName(destinationFilePath) ?? throw new NullReferenceException($"Directory of '{destinationFilePath}' is empty"); 
                Directory.CreateDirectory(outputDirectory);

                try
                {
                    File.Copy(fileToBackup, destinationFilePath, overwrite: true);
                    mLogger.LogInformation($"Copied '{fileToBackup}' to '{destinationFilePath}'");
                }
                catch (IOException ex)
                {
                    mLogger.LogError($"Failed to copy '{fileToBackup}' to '{destinationFilePath}'", ex);
                }

                mFilesHashesHandler.AddFileHash(fileHash, destinationFilePath);
            }
        }
    }
}