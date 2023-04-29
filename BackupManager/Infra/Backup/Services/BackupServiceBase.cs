using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.App;
using BackupManager.Domain.Mapping;
using BackupManager.Domain.Settings;
using Microsoft.Extensions.Logging;

namespace BackupManager.Infra.Backup.Services;

public abstract class BackupServiceBase : IBackupService
{
    protected readonly FilesHashesHandler mFilesHashesHandler;
    protected readonly ILogger<BackupServiceBase> mLogger;

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    protected BackupServiceBase(FilesHashesHandler filesHashesHandler, ILogger<BackupServiceBase> logger)
    {
        mFilesHashesHandler = filesHashesHandler ?? throw new ArgumentNullException(nameof(filesHashesHandler));
        mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    protected abstract void AddDirectoriesToSearchQueue(Queue<string> directoriesToSearch, string currentSearchDirectory);
    
    protected abstract IEnumerable<string> EnumerateFiles(string directory);

    protected abstract (string fileHash, bool isFileHashExist) GetFileHashData(string filePath);
    
    protected abstract void CopyFile(string fileToBackup, string destinationFilePath);
    
    protected abstract bool IsDirectoryExists(string directory);
    
    public void BackupFiles(BackupSettings backupSettings, CancellationToken cancellationToken)
    {
        // TODO dor handle cases of mShouldBackupToSelf.
        // TODO DOR Add tests for mShouldBackupToSelf.
        
        List<Task> backupTasks = new();
        
        foreach (DirectoriesMap directoriesMap in backupSettings.DirectoriesSourcesToDirectoriesDestinationMap)
        {
            mLogger.LogInformation($"Handling backup from '{directoriesMap.SourceRelativeDirectory}' to '{directoriesMap.DestRelativeDirectory}'");

            string sourceDirectoryToBackup = directoriesMap.SourceRelativeDirectory;
            if (backupSettings.RootDirectory is not null)
            {
                sourceDirectoryToBackup = Path.Combine(backupSettings.RootDirectory, directoriesMap.SourceRelativeDirectory);
            }
            
            Dictionary<string, string> filePathToFileHashMap = GetFilesToBackup(sourceDirectoryToBackup);

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

    private Dictionary<string, string> GetFilesToBackup(string directoryToBackup)
    {
        Dictionary<string, string> filePathToFileHashMap = new();
        
        if (!IsDirectoryExists(directoryToBackup))
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

            AddDirectoriesToSearchQueue(directoriesToSearch, currentSearchDirectory);
            AddFilesToBackupOnlyIfFileNotBackupedAlready(filePathToFileHashMap, currentSearchDirectory);
        }

        mLogger.LogInformation($"Finished iterative operation for finding updated files from '{directoryToBackup}'");
        return filePathToFileHashMap;
    }

    private static void UpdateLastBackupTime()
    {
        File.AppendAllText(Consts.BackupTimeDiaryFilePath,DateTime.Now + Environment.NewLine);
    }

    private void AddFilesToBackupOnlyIfFileNotBackupedAlready(IDictionary<string, string> filePathToFileHashMap, string directory)
    {
        foreach (string filePath in EnumerateFiles(directory))
        {
            (string fileHash, bool isFileHashExist) = GetFileHashData(filePath);
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
            string fileToBackupWithoutCharSeparators = fileToBackup.Trim(Path.DirectorySeparatorChar);
            string sourceRelativeDirectory = directoriesMap.SourceRelativeDirectory.TrimEnd(Path.DirectorySeparatorChar);
            string destinationDirectory = directoriesMap.DestRelativeDirectory.TrimEnd(Path.DirectorySeparatorChar);
            string destinationFilePath = fileToBackupWithoutCharSeparators.Replace(sourceRelativeDirectory, destinationDirectory);

            if (backupSettings.ShouldBackupToKnownDirectory)
            {
                if (backupSettings.RootDirectory is not null)
                {
                    destinationFilePath = destinationFilePath.Replace(backupSettings.RootDirectory, Consts.BackupsDirectoryPath);
                }
            }
            else
            {
                // TODO DOR handle.
            }
            
            string outputDirectory = Path.GetDirectoryName(destinationFilePath) ?? throw new NullReferenceException($"Directory of '{destinationFilePath}' is empty"); 
            _ = Directory.CreateDirectory(outputDirectory);

            try
            {
                CopyFile(fileToBackup, destinationFilePath);
                mLogger.LogInformation($"Copied '{fileToBackup}' to '{destinationFilePath}'");
                
                string relativeFilePathToBackup = Path.GetRelativePath(directoriesMap.SourceRelativeDirectory, fileToBackup);
                mFilesHashesHandler.AddFileHash(fileHash, relativeFilePathToBackup);
            }
            catch (IOException ex)
            {
                mLogger.LogError(ex, $"Failed to copy '{fileToBackup}' to '{destinationFilePath}'");
            }
        }
    }
}
