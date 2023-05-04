using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.App;
using BackupManager.Domain.Enums;
using BackupManager.Domain.Hash;
using BackupManager.Domain.Mapping;
using BackupManager.Domain.Settings;
using BackupManager.Infra.TasksRunners;
using Microsoft.Extensions.Logging;

namespace BackupManager.Infra.Backup.Services;

public abstract class BackupServiceBase : IBackupService
{
    protected readonly FilesHashesHandler mFilesHashesHandler;
    protected readonly ILogger<BackupServiceBase> mLogger;
    private readonly ILoggerFactory mLoggerFactory;

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    protected BackupServiceBase(FilesHashesHandler filesHashesHandler, ILoggerFactory loggerFactory)
    {
        mFilesHashesHandler = filesHashesHandler ?? throw new ArgumentNullException(nameof(filesHashesHandler));
        mLoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        mLogger = loggerFactory.CreateLogger<BackupServiceBase>();
    }
    
    protected abstract void AddDirectoriesToSearchQueue(Queue<string> directoriesToSearch, string currentSearchDirectory);
    
    protected abstract IEnumerable<string> EnumerateFiles(string directory);

    protected abstract (string? fileHash, bool isAlreadyBackuped) GetFileHashData(string filePath, SearchMethod searchMethod);
    
    protected abstract void CopyFile(string fileToBackup, string destinationFilePath);
    
    protected abstract bool IsDirectoryExists(string directory);
    
    public void BackupFiles(BackupSettings backupSettings, CancellationToken cancellationToken)
    {
        // TODO DOR now - handle bug - Copied '\Internal shared storage\VoiceRecorder\Recording_15.m4a' to 'C:/Program Files/BackupService/Data/Backups/Recording_15.m4a'
        // instaed of Copied '\Internal shared storage\VoiceRecorder\Recording_15.m4a' to 'C:/Program Files/BackupService/Data/Backups/VoiceRecorder/Recording_15.m4a'
        
        // TOdO DOR fix duplicates in Data.json like this:
        // "6259E4EAA218325E0F3BAE48621E926C936C79A992F5EF3A3B72817959DD8728": [
        // "\\VoiceRecorder\\Recording_9.m4a",
        // "\\VoiceRecorder\\Recording_9.m4a"
        //     ],
        
        // TODO dor handle cases of mShouldBackupToSelf.
        // TODO DOR Add tests for mShouldBackupToSelf.

        ushort numberOfParallelDirectoriesToCopy = backupSettings.AllowMultithreading ? (ushort)4 : (ushort)1; 
        TasksRunner tasksRunner = new(numberOfParallelDirectoriesToCopy, mLoggerFactory.CreateLogger<TasksRunner>()); 
        
        foreach (DirectoriesMap directoriesMap in backupSettings.DirectoriesSourcesToDirectoriesDestinationMap)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                mLogger.LogInformation($"Cancel requested");
                break;
            }
            
            mLogger.LogInformation($"Handling backup from '{directoriesMap.SourceRelativeDirectory}' to '{directoriesMap.DestRelativeDirectory}'");

            string sourceDirectoryToBackup = directoriesMap.SourceRelativeDirectory;
            if (backupSettings.RootDirectory is not null)
            {
                sourceDirectoryToBackup = Path.Combine(backupSettings.RootDirectory, directoriesMap.SourceRelativeDirectory);
            }
            
            Dictionary<string, string> filePathToFileHashMap = 
                GetFilesToBackup(sourceDirectoryToBackup, backupSettings.SearchMethod, cancellationToken);

            if (filePathToFileHashMap.Count == 0)
            {
                mLogger.LogInformation($"No files found to backup in '{directoriesMap.SourceRelativeDirectory}'");
                continue;
            }

            Task backupTask = 
                Task.Run(() =>
                {
                    BackupFiles(backupSettings, filePathToFileHashMap, directoriesMap, cancellationToken);
                    mFilesHashesHandler.Save();                    
                }, cancellationToken);
            
            tasksRunner.RunTask(backupTask, cancellationToken);
        }

        _ = tasksRunner.WaitAll(cancellationToken);
        UpdateLastBackupTime();
    }

    private Dictionary<string, string> GetFilesToBackup(string directoryToBackup, SearchMethod searchMethod, CancellationToken cancellationToken)
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
            if (cancellationToken.IsCancellationRequested)
            {
                mLogger.LogInformation($"Cancel requested");
                break;
            }
            
            string currentSearchDirectory = directoriesToSearch.Dequeue();
            mLogger.LogDebug($"Collecting files from directory '{currentSearchDirectory}'");

            AddDirectoriesToSearchQueue(directoriesToSearch, currentSearchDirectory);
            AddFilesToBackupOnlyIfFileNotBackupedAlready(filePathToFileHashMap, currentSearchDirectory, searchMethod, cancellationToken);
        }

        mLogger.LogInformation($"Finished iterative operation for finding updated files from '{directoryToBackup}'");
        return filePathToFileHashMap;
    }

    private static void UpdateLastBackupTime()
    {
        File.AppendAllText(Consts.BackupTimeDiaryFilePath,DateTime.Now + Environment.NewLine);
    }

    private void AddFilesToBackupOnlyIfFileNotBackupedAlready(IDictionary<string, string> filePathToFileHashMap,
        string directory,
        SearchMethod searchMethod,
        CancellationToken cancellationToken)
    {
        foreach (string filePath in EnumerateFiles(directory))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                mLogger.LogInformation($"Cancel requested");
                break;
            }
            
            (string? fileHash, bool isAlreadyBackuped) = GetFileHashData(filePath, searchMethod);
            if (isAlreadyBackuped)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(fileHash))
            {
                mLogger.LogError($"Hash for '{filePath}' was not calculated"); 
                continue;
            }
            
            filePathToFileHashMap.Add(filePath, fileHash);
            mLogger.LogInformation($"Found file '{filePath}' to backup");
        }
    }

    private void BackupFiles(BackupSettings backupSettings,
        Dictionary<string, string> filePathToBackupToFileHashMap,
        DirectoriesMap directoriesMap,
        CancellationToken cancellationToken)
    {
        mLogger.LogInformation($"Copying {filePathToBackupToFileHashMap.Count} files from '{directoriesMap.SourceRelativeDirectory}' to '{directoriesMap.DestRelativeDirectory}'");

        foreach ((string fileToBackup, string fileHash) in filePathToBackupToFileHashMap)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                mLogger.LogInformation($"Cancel requested");
                break;
            }
            
            string relativeFilePathToBackup = fileToBackup.Trim(Path.DirectorySeparatorChar)
                .Remove(0, (backupSettings.RootDirectory ?? string.Empty).Length);
            string destinationFilePath = buildDestinationFilePath(relativeFilePathToBackup, directoriesMap, backupSettings);
            
            string outputDirectory = Path.GetDirectoryName(destinationFilePath) ?? throw new NullReferenceException($"Directory of '{destinationFilePath}' is empty"); 
            _ = Directory.CreateDirectory(outputDirectory);

            try
            {
                CopyFile(fileToBackup, destinationFilePath);
                mLogger.LogInformation($"Copied '{fileToBackup}' to '{destinationFilePath}'");
                
                mFilesHashesHandler.AddFileHash(fileHash, relativeFilePathToBackup);
            }
            catch (IOException ex)
            {
                mLogger.LogError(ex, $"Failed to copy '{fileToBackup}' to '{destinationFilePath}'");
            }
        }
    }

    private string buildDestinationFilePath(string relativeSourceFilePath,
        DirectoriesMap directoriesMap,
        BackupSettings backupSettings)
    {
        string relativeDestinationFilePath = buildRelativeDestinationFilePath(relativeSourceFilePath, directoriesMap);
        
        // TODO DOR handle not case of backupSettings.ShouldBackupToKnownDirectory.
        string rootDirectoryPath = backupSettings.ShouldBackupToKnownDirectory ? Consts.BackupsDirectoryPath : "TOdo DOR";

        return Path.Combine(rootDirectoryPath, relativeDestinationFilePath);
    }

    private string buildRelativeDestinationFilePath(string relativeSourceFilePath, DirectoriesMap directoriesMap)
    {
        string fixedRelativeSourceFilePath = relativeSourceFilePath.Trim(Path.DirectorySeparatorChar).Replace('\\', '/');
        
        if (string.IsNullOrWhiteSpace(directoriesMap.DestRelativeDirectory))
        {
            return fixedRelativeSourceFilePath;
        }
        
        string relativeSourceDirectory = directoriesMap.SourceRelativeDirectory.Trim(Path.DirectorySeparatorChar).Replace('\\', '/');
        string relativeDestinationDirectory = directoriesMap.DestRelativeDirectory.Trim(Path.DirectorySeparatorChar).Replace('\\', '/');
        string relativeDestinationFilePath = fixedRelativeSourceFilePath.Replace(relativeSourceDirectory, relativeDestinationDirectory);
        return relativeDestinationFilePath;
    }
}
