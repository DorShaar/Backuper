using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.App;
using BackupManager.Domain.Mapping;
using BackupManager.Domain.Settings;
using Microsoft.Extensions.Logging;

namespace BackupManager.Infra;

public class BackupService : IBackupService
{
    private readonly FilesHashesHandler mFilesHashesHandler;
    private readonly ILogger<BackupService> mLogger;

    public BackupService(FilesHashesHandler filesHashesHandler, ILogger<BackupService> logger)
    {
        mFilesHashesHandler = filesHashesHandler ?? throw new ArgumentNullException(nameof(filesHashesHandler));
        mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // TODO DOR Add tests.
    public void BackupFiles(BackupSettings backupSettings, CancellationToken cancellationToken)
    {
        // TODO dor handle cases of mShouldBackupToSelf.
        
        List<Task> backupTasks = new();
        
        foreach (DirectoriesMap directoriesMap in backupSettings.DirectoriesSourcesToDirectoriesDestinationMap)
        {
            mLogger.LogInformation($"Handling backup from '{directoriesMap.SourceRelativeDirectory}' to '{directoriesMap.DestRelativeDirectory}'");

            string sourceDirectoryToBackup = directoriesMap.SourceRelativeDirectory;
            if (backupSettings.RootDirectory is not null)
            {
                sourceDirectoryToBackup = Path.Combine(backupSettings.RootDirectory, directoriesMap.SourceRelativeDirectory);
            }
            
            Dictionary<string, string> filePathToFileHashMap = getFilesToBackup(sourceDirectoryToBackup);

            if (filePathToFileHashMap.Count == 0)
            {
                mLogger.LogInformation($"No files found to backup in '{directoriesMap.SourceRelativeDirectory}'");
                continue;
            }

            Task backupTask = Task.Run(() => BackupFiles(backupSettings, filePathToFileHashMap, directoriesMap), cancellationToken);
            backupTasks.Add(backupTask);
        }

        Task.WaitAll(backupTasks.ToArray(), cancellationToken);

        mFilesHashesHandler.Save();
        UpdateLastBackupTime();
    }

    private static void UpdateLastBackupTime()
    {
        File.AppendAllText(Consts.BackupTimeDiaryFilePath,DateTime.Now + Environment.NewLine);
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

    private void BackupFiles(BackupSettings backupSettings,
        Dictionary<string, string> filePathToBackupToFileHashMap,
        DirectoriesMap directoriesMap)
    {
        mLogger.LogInformation($"Copying {filePathToBackupToFileHashMap.Count} files from '{directoriesMap.SourceRelativeDirectory}' to '{directoriesMap.DestRelativeDirectory}'");
        foreach ((string fileToBackup, string fileHash) in filePathToBackupToFileHashMap)
        {
            string destinationFilePath = fileToBackup.Replace($"{Path.DirectorySeparatorChar}{directoriesMap.SourceRelativeDirectory}{Path.DirectorySeparatorChar}",
            $"{Path.DirectorySeparatorChar}{directoriesMap.DestRelativeDirectory}{Path.DirectorySeparatorChar}");

            if (backupSettings.ShouldBackupToKnownDirectory)
            {
                destinationFilePath = Consts.DataDirectoryPath;

                if (backupSettings.RootDirectory is not null)
                {
                    destinationFilePath = destinationFilePath.Replace(backupSettings.RootDirectory, Consts.DataDirectoryPath);
                }
            }
            else
            {
                // TODO DOR handle.
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
                mLogger.LogError(ex, $"Failed to copy '{fileToBackup}' to '{destinationFilePath}'");
            }

            mFilesHashesHandler.AddFileHash(fileHash, destinationFilePath);
        }
    }
}
