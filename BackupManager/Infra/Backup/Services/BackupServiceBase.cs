﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.App.Backup.Services;
using BackupManager.Domain.Enums;
using BackupManager.Domain.Hash;
using BackupManager.Domain.Mapping;
using BackupManager.Domain.Settings;
using IOWrapper;
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

    protected abstract (string? fileHash, bool isAlreadyBackuped) GetFileHashData(string filePath, string relativeFilePath, SearchMethod searchMethod);
    
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

            string sourceDirectoryToBackup = BuildSourceDirectoryToBackup(backupSettings, directoriesMap.SourceRelativeDirectory);
            Dictionary<FileSystemPath, string> filePathToFileHashMap =
                GetFilesToBackup(sourceDirectoryToBackup, backupSettings, cancellationToken);

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
    
    private static string BuildSourceDirectoryToBackup(BackupSettings backupSettings, string sourceRelativeDirectory)
    {
        return Path.Combine(backupSettings.ShouldBackupToKnownDirectory
            ? backupSettings.RootDirectory 
            : Consts.BackupsDirectoryPath, sourceRelativeDirectory);
    }

    private Dictionary<FileSystemPath, string> GetFilesToBackup(string directoryToBackup, BackupSettings backupSettings, CancellationToken cancellationToken)
    {
        Dictionary<FileSystemPath, string> filePathToFileHashMap = new();
        
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
            AddFilesToBackupOnlyIfFileNotBackedUpAlready(filePathToFileHashMap,
                currentSearchDirectory,
                backupSettings,
                cancellationToken);
        }

        mLogger.LogInformation($"Finished iterative operation for finding updated files from '{directoryToBackup}'");
        return filePathToFileHashMap;
    }

    private static void UpdateLastBackupTime(string? backupDescription)
    {
        File.AppendAllText(Consts.BackupTimeDiaryFilePath, $"{DateTime.Now} --- {backupDescription}" + Environment.NewLine);
    }

    private void AddFilesToBackupOnlyIfFileNotBackedUpAlready(IDictionary<FileSystemPath, string> filePathToFileHashMap,
        string directory,
        BackupSettings backupSettings,
        CancellationToken cancellationToken)
    {
        foreach (string filePath in EnumerateFiles(directory))
        {
            FileSystemPath fileSystemPath = new(filePath);
            string relativeTo = backupSettings.ShouldBackupToKnownDirectory ? backupSettings.RootDirectory : Consts.BackupsDirectoryPath;
            FileSystemPath relativeFilePath = fileSystemPath.GetRelativePath(relativeTo);                
            
            if (cancellationToken.IsCancellationRequested)
            {
                mLogger.LogInformation($"Cancel requested");
                break;
            }
            
            (string? fileHash, bool isAlreadyBackedUp) = GetFileHashData(filePath, relativeFilePath.PathString, backupSettings.SearchMethod);
            if (isAlreadyBackedUp)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(fileHash))
            {
                mLogger.LogError($"Hash for '{filePath}' was not calculated");
                continue;
            }

            filePathToFileHashMap.Add(fileSystemPath, fileHash);
            mLogger.LogInformation($"Found file '{fileSystemPath}' to backup");
        }
    }

    private void BackupFilesInternal(BackupSettings backupSettings,
        Dictionary<FileSystemPath, string> filePathToBackupToFileHashMap,
        DirectoriesMap directoriesMap,
        CancellationToken cancellationToken)
    {
        mLogger.LogInformation(
            $"Copying {filePathToBackupToFileHashMap.Count} files from '{directoriesMap.SourceRelativeDirectory}' to '{directoriesMap.DestRelativeDirectory}'");

        FileSystemPath destinationDirectoryPath = BuildDestinationDirectoryPath(backupSettings);
        
        foreach ((FileSystemPath fileToBackup, string fileHash) in filePathToBackupToFileHashMap)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                mLogger.LogInformation($"Cancel requested");
                break;
            }

            FileSystemPath relativeFilePathToBackup = BuildRelativeSourceFilePath(fileToBackup, backupSettings);
            FileSystemPath destinationFilePath = BuildDestinationFilePath(relativeFilePathToBackup, destinationDirectoryPath, directoriesMap);
            
            string outputDirectory = Path.GetDirectoryName(destinationFilePath.PathString)
                                     ?? throw new NullReferenceException($"Directory of '{destinationFilePath}' is empty"); 
            _ = Directory.CreateDirectory(outputDirectory);

            try
            {
                CopyFile(fileToBackup.PathString, destinationFilePath.PathString);
                mLogger.LogInformation($"Copied '{fileToBackup}' to '{destinationFilePath}'");
                
                mFilesHashesHandler.AddFileHash(fileHash, relativeFilePathToBackup.PathString);
            }
            catch (IOException ex)
            {
                mLogger.LogError(ex, $"Failed to copy '{fileToBackup}' to '{destinationFilePath}'");
            }
        }
    }

    private static FileSystemPath BuildDestinationDirectoryPath(BackupSettings backupSettings)
    {
        if (backupSettings.ShouldBackupToKnownDirectory)
        {
            return new FileSystemPath(Consts.BackupsDirectoryPath);
        }

        if (string.IsNullOrWhiteSpace(backupSettings.RootDirectory))
        {
            throw new NullReferenceException(
                $"{nameof(backupSettings.RootDirectory)} is empty while {nameof(backupSettings.ShouldBackupToKnownDirectory)} is {backupSettings.ShouldBackupToKnownDirectory}");
        }

        return new FileSystemPath(backupSettings.RootDirectory);
    }
    
    private FileSystemPath BuildDestinationFilePath(FileSystemPath relativeSourceFilePath,
        FileSystemPath destinationDirectoryPath,
        DirectoriesMap directoriesMap)
    {
        FileSystemPath relativeDestinationFilePath = BuildRelativeDestinationFilePath(relativeSourceFilePath, directoriesMap);
        return destinationDirectoryPath.Combine(relativeDestinationFilePath);
    }

    private static FileSystemPath BuildRelativeDestinationFilePath(FileSystemPath relativeSourceFilePath, DirectoriesMap directoriesMap)
    {
        if (string.IsNullOrWhiteSpace(directoriesMap.DestRelativeDirectory))
        {
            return relativeSourceFilePath;
        }
        
        FileSystemPath relativeDestinationFilePath = relativeSourceFilePath.Replace(
            directoriesMap.SourceRelativeDirectory, directoriesMap.DestRelativeDirectory);
        return relativeDestinationFilePath;
    }

    private FileSystemPath BuildRelativeSourceFilePath(FileSystemPath sourceFilePath, BackupSettings backupSettings)
    {
        if (backupSettings.ShouldBackupToKnownDirectory)
        {
            return string.IsNullOrWhiteSpace(backupSettings.RootDirectory) 
                ? sourceFilePath
                : sourceFilePath.GetRelativePath(backupSettings.RootDirectory);
        }
        
        return sourceFilePath.GetRelativePath(Consts.BackupsDirectoryPath);
    }
}
