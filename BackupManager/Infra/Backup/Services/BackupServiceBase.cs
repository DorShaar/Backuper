using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.App.Backup.Services;
using BackupManager.App.Database;
using BackupManager.Domain.Enums;
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

    protected abstract Task<(string? fileHash, bool isAlreadyBackuped)> GetFileHashData(string filePath, string relativeFilePath, SearchMethod searchMethod, CancellationToken cancellationToken);
    
    protected abstract void CopyFile(string fileToBackup, string destinationFilePath);
    
    protected abstract bool IsDirectoryExists(string directory);
    
    public async Task BackupFiles(BackupSettings backupSettings, CancellationToken cancellationToken)
    {
        mLogger.LogInformation($"Start backup '{backupSettings.Description ?? backupSettings.ToString()}'");

        await LoadDatabase(backupSettings, cancellationToken).ConfigureAwait(false);

        if (!backupSettings.ShouldBackupToKnownDirectory)
        {
            await MapAllFilesWithHash(backupSettings, cancellationToken).ConfigureAwait(false);
        }
        
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
            bool isGetAllFilesCompleted = false;
            ushort iteration = 0;
            while (!isGetAllFilesCompleted)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    mLogger.LogInformation($"Cancel requested");
                    break;
                }

                iteration++;
                mLogger.LogInformation($"Handling backup from '{directoriesMap.SourceRelativeDirectory}' to '{directoriesMap.DestRelativeDirectory}'. Iteration number: {iteration}");

                string sourceDirectoryToBackup = BuildSourceDirectoryToBackup(backupSettings, directoriesMap.SourceRelativeDirectory);
                string relativeDirectory = backupSettings.ShouldBackupToKnownDirectory ? backupSettings.RootDirectory : Consts.BackupsDirectoryPath;
                (Dictionary<FileSystemPath, string> filePathToFileHashMap, isGetAllFilesCompleted) =
                    await GetFilesToBackup(sourceDirectoryToBackup, relativeDirectory, backupSettings, iteration, cancellationToken).ConfigureAwait(false);

                if (filePathToFileHashMap.Count == 0)
                {
                    mLogger.LogInformation($"No files found to backup in '{directoriesMap.SourceRelativeDirectory}'");
                    continue;
                }

                // LongRunning hints the task scheduler to create new thread.
                Task backupTask = Task.Factory.StartNew(async () =>
                {
                    await BackupFilesInternal(backupSettings, filePathToFileHashMap, directoriesMap, cancellationToken).ConfigureAwait(false);
                }, TaskCreationOptions.LongRunning);
            
                await tasksRunner.RunTask(backupTask, cancellationToken).ConfigureAwait(false);
            }
        }

        _ = await tasksRunner.WaitAll(cancellationToken).ConfigureAwait(false);
        UpdateLastBackupTime(backupSettings.Description);
        
        mLogger.LogInformation($"Finished backup '{backupSettings.Description ?? backupSettings.ToString()}'");
    }

    private async Task LoadDatabase(BackupSettings backupSettings, CancellationToken cancellationToken)
    {
        string databaseName = Consts.BackupFilesCollectionName;
        if (!backupSettings.ShouldBackupToKnownDirectory)
        {
            databaseName = string.Format(Consts.BackupFilesForKnownDriveCollectionTemplate, backupSettings.Token);
        }
        
        await mFilesHashesHandler.LoadDatabase(databaseName, cancellationToken).ConfigureAwait(false);
    }
    
    private async Task MapAllFilesWithHash(BackupSettings backupSettings, CancellationToken cancellationToken)
    {
        mLogger.LogInformation($"Before backup we should verify all files in {backupSettings.RootDirectory} are mapped");

        ushort iteration = 0;
        bool isGetAllFilesCompleted = false;
        while (!isGetAllFilesCompleted)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                mLogger.LogInformation($"Cancel requested");
                break;
            }

            iteration++;
            mLogger.LogInformation($"Mapping all files with hash from directory '{backupSettings.RootDirectory}'. Iteration number: {iteration}");

            (Dictionary<FileSystemPath, string> filePathToFileHashMap, isGetAllFilesCompleted) =
                await GetFilesToBackup(backupSettings.RootDirectory,
                                       relativeDirectory: backupSettings.RootDirectory,
                                       backupSettings,
                                       iteration,
                                       cancellationToken).ConfigureAwait(false);

            if (filePathToFileHashMap.Count == 0)
            {
                mLogger.LogDebug("No new files to map found");
                continue;
            }

            foreach ((FileSystemPath fileSystemPath, string fileHash) in filePathToFileHashMap)
            {
                await mFilesHashesHandler.AddFileHash(fileHash, fileSystemPath.PathString, cancellationToken).ConfigureAwait(false);
            }

            await mFilesHashesHandler.Save(cancellationToken).ConfigureAwait(false);
        }
    }
    
    private static string BuildSourceDirectoryToBackup(BackupSettings backupSettings, string sourceRelativeDirectory)
    {
        return Path.Combine(backupSettings.ShouldBackupToKnownDirectory
            ? backupSettings.RootDirectory 
            : Consts.BackupsDirectoryPath, sourceRelativeDirectory);
    }

    private async Task<(Dictionary<FileSystemPath, string>, bool)> GetFilesToBackup(string directoryToBackup,
                                                                                    string relativeDirectory,
                                                                                    BackupSettings backupSettings,
                                                                                    ushort iteration,
                                                                                    CancellationToken cancellationToken)
    {
        bool isGetAllFilesCompleted = false;
        Dictionary<FileSystemPath, string> filePathToFileHashMap = new();
        
        if (!IsDirectoryExists(directoryToBackup))
        {
            mLogger.LogError($"'{directoryToBackup}' does not exists");
            return (filePathToFileHashMap, true);
        }

        mLogger.LogInformation(iteration > 1 
                                   ? $"Continue getting files to backup from '{directoryToBackup}'. Iteration Number {iteration}"
                                   : $"Starting iterative search to find files to backup from '{directoryToBackup}'");

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
            isGetAllFilesCompleted = await AddFilesToBackupOnlyIfFileNotBackedUpAlready(filePathToFileHashMap,
                                                                                        currentSearchDirectory,
                                                                                        relativeDirectory,
                                                                                        backupSettings,
                                                                                        cancellationToken).ConfigureAwait(false);
            if (!isGetAllFilesCompleted)
            {
                break;
            }
        }

        mLogger.LogInformation(isGetAllFilesCompleted 
                                   ? $"Finished iterative search for finding files from '{directoryToBackup}'" 
                                   : $"Paused iterative search in '{directoriesToSearch}'");
        return (filePathToFileHashMap, isGetAllFilesCompleted);
    }

    private static void UpdateLastBackupTime(string? backupDescription)
    {
        File.AppendAllText(Consts.BackupTimeDiaryFilePath, $"{DateTime.Now} --- {backupDescription}" + Environment.NewLine);
    }

    private async Task<bool> AddFilesToBackupOnlyIfFileNotBackedUpAlready(IDictionary<FileSystemPath, string> filePathToFileHashMap,
        string directory,
        string relativeDirectory,
        BackupSettings backupSettings,
        CancellationToken cancellationToken)
    {
        bool isGetAllFilesCompleted = false;
        foreach (string filePath in EnumerateFiles(directory))
        {
            FileSystemPath fileSystemPath = new(filePath);
            FileSystemPath relativeFilePath = fileSystemPath.GetRelativePath(relativeDirectory);
            
            if (cancellationToken.IsCancellationRequested)
            {
                mLogger.LogInformation($"Cancel requested");
                break;
            }
            
            (string? fileHash, bool isAlreadyBackedUp) = await GetFileHashData(filePath, relativeFilePath.PathString, backupSettings.SearchMethod, cancellationToken).ConfigureAwait(false);
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
            
            if (filePathToFileHashMap.Count >= backupSettings.SaveInterval)
            {
                mLogger.LogInformation($"Collected {filePathToFileHashMap.Count} files, pausing file fetch");
                return isGetAllFilesCompleted;
            }
        }

        isGetAllFilesCompleted = true;
        return isGetAllFilesCompleted;
    }

    private async Task BackupFilesInternal(BackupSettings backupSettings,
        Dictionary<FileSystemPath, string> filePathToBackupToFileHashMap,
        DirectoriesMap directoriesMap,
        CancellationToken cancellationToken)
    {
        mLogger.LogInformation(
            $"Copying {filePathToBackupToFileHashMap.Count} files from '{directoriesMap.SourceRelativeDirectory}' to '{directoriesMap.DestRelativeDirectory}'");

        FileSystemPath destinationDirectoryPath = BuildDestinationDirectoryPath(backupSettings);

        ushort backupFilesIntervalCount = 0;
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
                
                await mFilesHashesHandler.AddFileHash(fileHash, relativeFilePathToBackup.PathString, cancellationToken).ConfigureAwait(false);

                backupFilesIntervalCount++;
                if (backupFilesIntervalCount % backupSettings.SaveInterval == 0)
                {
                    mLogger.LogDebug($"Reached save interval {backupFilesIntervalCount}");
                    await mFilesHashesHandler.Save(cancellationToken).ConfigureAwait(false);
                    backupFilesIntervalCount = 0;
                }
            }
            catch (IOException ex)
            {
                mLogger.LogError(ex, $"Failed to copy '{fileToBackup}' to '{destinationFilePath}'");
            }
        }
        
        mLogger.LogInformation($"Done copy {filePathToBackupToFileHashMap.Count} files from '{directoriesMap.SourceRelativeDirectory}' to '{directoriesMap.DestRelativeDirectory}'");
        await mFilesHashesHandler.Save(cancellationToken).ConfigureAwait(false);
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
