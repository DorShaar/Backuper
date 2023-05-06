using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.App.Backup.Services;
using BackupManager.Domain.Enums;
using BackupManager.Domain.Hash;
using BackupManager.Domain.Mapping;
using BackupManager.Domain.Settings;
using Microsoft.Extensions.Logging;
using SmartTasks;

namespace BackupManager.Infra.Backup.Services;

public abstract class BackupServiceBase : IBackupService
{
    protected readonly IFilesHashesHandler mFilesHashesHandler;
    protected readonly ILogger<BackupServiceBase> mLogger;
    private readonly ILoggerFactory mLoggerFactory;
    private readonly TimeSpan mIntervalForCheckingAvailableSlot = TimeSpan.FromMilliseconds(500);

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    protected BackupServiceBase(IFilesHashesHandler filesHashesHandler, ILoggerFactory loggerFactory)
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
    
    public async Task BackupFiles(BackupSettings backupSettings, CancellationToken cancellationToken)
    {
        mLogger.LogInformation($"Start backup '{backupSettings.Description}'");
        
        ushort numberOfParallelDirectoriesToCopy = backupSettings.AllowMultithreading ? (ushort)4 : (ushort)1;

        TasksRunnerConfigurations tasksRunnerConfigurations = new()
        {
            AllowedParallelTasks = numberOfParallelDirectoriesToCopy,
            IntervalForCheckingAvailableSlot = mIntervalForCheckingAvailableSlot,
            NoAvailableSlotLogInterval = 10000,
            Logger = mLoggerFactory.CreateLogger<TasksRunner>()
        };

        TasksRunner tasksRunner = new(tasksRunnerConfigurations);
        
        foreach (DirectoriesMap directoriesMap in backupSettings.DirectoriesSourcesToDirectoriesDestinationMap)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                mLogger.LogInformation($"Cancel requested");
                break;
            }
            
            mLogger.LogInformation($"Handling backup from '{directoriesMap.SourceRelativeDirectory}' to '{directoriesMap.DestRelativeDirectory}'");

            string sourceDirectoryToBackup = buildSourceDirectoryToBackup(backupSettings, directoriesMap.SourceRelativeDirectory);
            Dictionary<string, string> filePathToFileHashMap =
                GetFilesToBackup(sourceDirectoryToBackup, backupSettings.SearchMethod, cancellationToken);

            if (filePathToFileHashMap.Count == 0)
            {
                mLogger.LogInformation($"No files found to backup in '{directoriesMap.SourceRelativeDirectory}'");
                continue;
            }

            // LongRunning hints the task scheduler to create new thread.
            Task backupTask = Task.Factory.StartNew(async () =>
            {
                BackupFilesInternal(backupSettings, filePathToFileHashMap, directoriesMap, cancellationToken);
                await mFilesHashesHandler.Save(cancellationToken).ConfigureAwait(false);
            }, TaskCreationOptions.LongRunning);
            
            await tasksRunner.RunTask(backupTask, cancellationToken).ConfigureAwait(false);
        }

        _ = await tasksRunner.WaitAll(cancellationToken).ConfigureAwait(false);
        UpdateLastBackupTime(backupSettings.Description);
        
        mLogger.LogInformation($"Finished backup '{backupSettings.Description}'");
    }
    
    private static string buildSourceDirectoryToBackup(BackupSettings backupSettings, string sourceRelativeDirectory)
    {
        if (backupSettings.RootDirectory is not null)
        {
            return Path.Combine(backupSettings.ShouldBackupToKnownDirectory
                ? backupSettings.RootDirectory 
                : Consts.BackupsDirectoryPath, sourceRelativeDirectory);
        }

        return sourceRelativeDirectory;
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

    private static void UpdateLastBackupTime(string? backupDescription)
    {
        File.AppendAllText(Consts.BackupTimeDiaryFilePath, $"{DateTime.Now} --- {backupDescription}" + Environment.NewLine);
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

    private void BackupFilesInternal(BackupSettings backupSettings,
        Dictionary<string, string> filePathToBackupToFileHashMap,
        DirectoriesMap directoriesMap,
        CancellationToken cancellationToken)
    {
        mLogger.LogInformation($"Copying {filePathToBackupToFileHashMap.Count} files from '{directoriesMap.SourceRelativeDirectory}' to '{directoriesMap.DestRelativeDirectory}'");

        string destinationDirectoryPath = buildDestinationDirectoryPath(backupSettings);
        
        foreach ((string fileToBackup, string fileHash) in filePathToBackupToFileHashMap)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                mLogger.LogInformation($"Cancel requested");
                break;
            }

            string relativeFilePathToBackup = BuildRelativeSourceFilePath(fileToBackup, backupSettings);
            string destinationFilePath = BuildDestinationFilePath(relativeFilePathToBackup, destinationDirectoryPath, directoriesMap);
            
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

    private string buildDestinationDirectoryPath(BackupSettings backupSettings)
    {
        if (backupSettings.ShouldBackupToKnownDirectory)
        {
            return Consts.BackupsDirectoryPath;
        }

        if (string.IsNullOrWhiteSpace(backupSettings.RootDirectory))
        {
            throw new NullReferenceException(
                $"{nameof(backupSettings.RootDirectory)} is empty while {nameof(backupSettings.ShouldBackupToKnownDirectory)} is {backupSettings.ShouldBackupToKnownDirectory}");
        }

        return backupSettings.RootDirectory;
    }
    
    private string BuildDestinationFilePath(string relativeSourceFilePath,
        string destinationDirectoryPath,
        DirectoriesMap directoriesMap)
    {
        string relativeDestinationFilePath = BuildRelativeDestinationFilePath(relativeSourceFilePath, directoriesMap);
        return Path.Combine(destinationDirectoryPath, relativeDestinationFilePath);
    }

    private string BuildRelativeDestinationFilePath(string relativeSourceFilePath, DirectoriesMap directoriesMap)
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

    private string BuildRelativeSourceFilePath(string sourceFilePath, BackupSettings backupSettings)
    {
        int charactersNumberToTrimFromBeginning = backupSettings.ShouldBackupToKnownDirectory
            ? (backupSettings.RootDirectory ?? string.Empty).Length
            : Consts.BackupsDirectoryPath.Length;  
        
        return sourceFilePath.Trim(Path.DirectorySeparatorChar)
                             .Remove(0, charactersNumberToTrimFromBeginning);
    }
}
