using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.App.Backup.Services;
using BackupManager.App.Database;
using BackupManager.Domain.Enums;
using BackupManager.Domain.Settings;
using BackupManagerCore;
using BackupManagerCore.Mapping;
using IOWrapper;
using Microsoft.Extensions.Logging;

namespace BackupManager.Infra.Backup.Services;

public abstract class BackupServiceBase : IBackupService
{
    protected readonly IFilesHashesHandler _filesHashesHandler;
    protected readonly ILogger<BackupServiceBase> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    protected BackupServiceBase(IFilesHashesHandler filesHashesHandler, ILoggerFactory loggerFactory)
    {
        _filesHashesHandler = filesHashesHandler ?? throw new ArgumentNullException(nameof(filesHashesHandler));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = loggerFactory.CreateLogger<BackupServiceBase>();
    }
    
    protected abstract void AddSubDirectoriesToSearchQueue(Queue<string> directoriesToSearch, string currentSearchDirectory);
    
    protected abstract IEnumerable<string> EnumerateFiles(string directory);

    protected abstract Task<(string?, bool)> GetFileHashData(string filePath, string relativeFilePath, SearchMethod searchMethod, CancellationToken cancellationToken);
    
    protected abstract void CopyFile(string fileToBackup, string destinationFilePath);
    
    protected abstract bool IsDirectoryExists(string directory);
    
    public async Task BackupFiles(BackupSettings backupSettings, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Start backup '{backupSettings.Description ?? backupSettings.ToString()}'");

        await LoadDatabase(backupSettings, cancellationToken).ConfigureAwait(false);

        if (!backupSettings.ShouldBackupToKnownDirectory && backupSettings.ShouldMapFiles)
        {
            await MapAllFilesWithHash(backupSettings, cancellationToken).ConfigureAwait(false);
        }
        
        foreach (DirectoriesMap directoriesMap in backupSettings.DirectoriesSourcesToDirectoriesDestinationMap)
        {
            await BackupInternal(directoriesMap, backupSettings, cancellationToken).ConfigureAwait(false);
        }

        await UpdateLastBackupTime(backupSettings.Description).ConfigureAwait(false);
        
        _logger.LogInformation($"Finished backup '{backupSettings.Description ?? backupSettings.ToString()}'");

        WarnForNonBackupFiles(); 
    }

    private async Task LoadDatabase(BackupSettings backupSettings, CancellationToken cancellationToken)
    {
        string databaseName = Consts.Database.BackupFilesCollectionName;
        if (!backupSettings.ShouldBackupToKnownDirectory)
        {
            databaseName = string.Format(Consts.Database.BackupFilesForKnownDriveCollectionTemplate, backupSettings.Token);
        }
        
        await _filesHashesHandler.LoadDatabase(databaseName, cancellationToken).ConfigureAwait(false);
    }
    
    private async Task MapAllFilesWithHash(BackupSettings backupSettings, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Before backup we should verify all files in {backupSettings.RootDirectory} are mapped");

        ushort iteration = 0;
        bool isGetAllFilesCompleted = false;
        HashSet<string> alreadyCompletedDirectories = new();
        while (!isGetAllFilesCompleted)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Cancel requested");
                break;
            }

            iteration++;
            _logger.LogInformation($"Mapping all files with hash from directory '{backupSettings.RootDirectory}'. Iteration number: {iteration}");

            FilesBackupData filesBackupData = await GetFilesBackupData(backupSettings.RootDirectory,
                                                                       sourceRelativeDirectory: backupSettings.RootDirectory,
                                                                       backupSettings,
                                                                       alreadyCompletedDirectories,
                                                                       iteration,
                                                                       cancellationToken).ConfigureAwait(false);
            isGetAllFilesCompleted = filesBackupData.IsGetAllFilesCompleted;
            
            if (filesBackupData.NotBackedUpFilePathToFileHashMap is null || filesBackupData.NotBackedUpFilePathToFileHashMap.Count == 0)
            {
                _logger.LogDebug("No new files to map found");
                continue;
            }

            foreach ((FileSystemPath fileSystemPath, string fileHash) in filesBackupData.NotBackedUpFilePathToFileHashMap)
            {
                FileSystemPath relativeDestinationPath = fileSystemPath.GetRelativePath(backupSettings.RootDirectory);
                await _filesHashesHandler.AddFileHash(fileHash, relativeDestinationPath.PathString, cancellationToken).ConfigureAwait(false);
            }

            await _filesHashesHandler.Save(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task BackupInternal(DirectoriesMap directoriesMap, BackupSettings backupSettings, CancellationToken cancellationToken)
    {
        bool isGetAllFilesCompleted = false;
        ushort iteration = 0;
        HashSet<string> alreadyCompletedDirectories = [];

        while (!isGetAllFilesCompleted)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Cancel requested");
                break;
            }

            iteration++;
            _logger.LogInformation($"Handling backup from '{directoriesMap.SourceRelativeDirectory}' to '{directoriesMap.DestRelativeDirectory}'. Iteration number: {iteration}");

            string sourceDirectoryToBackup = BuildSourceDirectoryToBackup(backupSettings, directoriesMap.SourceRelativeDirectory);
            string sourceRelativeDirectory = backupSettings.ShouldBackupToKnownDirectory ? backupSettings.RootDirectory : Consts.Data.ReadyToBackupDirectoryPath;
            FilesBackupData filesBackupData = await GetFilesBackupData(sourceDirectoryToBackup,
                                                                       sourceRelativeDirectory,
                                                                       backupSettings,
                                                                       alreadyCompletedDirectories,
                                                                       iteration,
                                                                       cancellationToken).ConfigureAwait(false);
            isGetAllFilesCompleted = filesBackupData.IsGetAllFilesCompleted;

            if (filesBackupData.NotBackedUpFilePathToFileHashMap is not null && filesBackupData.NotBackedUpFilePathToFileHashMap.Count > 0)
            {
                await BackupFilesInternal(backupSettings,
                                          filesBackupData.NotBackedUpFilePathToFileHashMap,
                                          directoriesMap,
                                          cancellationToken).ConfigureAwait(false);
            }

            if (filesBackupData.AlreadyBackedUpFilePathToFileHashMap is not null && filesBackupData.AlreadyBackedUpFilePathToFileHashMap.Count > 0)
            {
                MoveFilesToBackedUpDirectoryIfRequired(backupSettings, filesBackupData.AlreadyBackedUpFilePathToFileHashMap);
            }
        }
    }
    
    private static string BuildSourceDirectoryToBackup(BackupSettings backupSettings, string sourceRelativeDirectory)
    {
        return backupSettings.ShouldBackupToKnownDirectory 
                   ? Path.Combine(backupSettings.RootDirectory, sourceRelativeDirectory)
                   : Path.Combine(Consts.Data.ReadyToBackupDirectoryPath, sourceRelativeDirectory);
    }

    private async Task<FilesBackupData> GetFilesBackupData(string directoryToBackup,
                                                           string sourceRelativeDirectory,
                                                           BackupSettings backupSettings,
                                                           ISet<string> alreadyCompletedDirectories,
                                                           ushort iteration,
                                                           CancellationToken cancellationToken)
    {
        bool isGetAllFilesCompleted = false;
        
        if (!IsDirectoryExists(directoryToBackup))
        {
            _logger.LogInformation($"'{directoryToBackup}' does not exists");
            return new FilesBackupData
            {
                IsGetAllFilesCompleted = true,
                AlreadyBackedUpFilePathToFileHashMap = null,
                NotBackedUpFilePathToFileHashMap = null
            };
        }

        _logger.LogInformation(iteration > 1 
                                   ? $"Continue getting files to backup from '{directoryToBackup}'. Iteration Number {iteration}"
                                   : $"Starting iterative search to find files to backup from '{directoryToBackup}'");

        Queue<string> directoriesToSearch = new();
        directoriesToSearch.Enqueue(directoryToBackup);

        Dictionary<FileSystemPath, string> notBackedUpFilePathToFileHashMap = new();
        Dictionary<FileSystemPath, string> alreadyBackedUpFilePathToFileHashMap = new();
        while (directoriesToSearch.Count > 0)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation($"Cancel requested");
                break;
            }

            string currentSearchDirectory = directoriesToSearch.Dequeue();
            AddSubDirectoriesToSearchQueue(directoriesToSearch, currentSearchDirectory);

            if (alreadyCompletedDirectories.Contains(currentSearchDirectory))
            {
                continue;
            }
            
            _logger.LogDebug($"Collecting files from directory '{currentSearchDirectory}'");
            isGetAllFilesCompleted = await AddFilesToBackupOnlyIfFileNotBackedUpAlready(notBackedUpFilePathToFileHashMap,
                                                                                        alreadyBackedUpFilePathToFileHashMap,
                                                                                        currentSearchDirectory,
                                                                                        sourceRelativeDirectory,
                                                                                        backupSettings,
                                                                                        cancellationToken).ConfigureAwait(false);
            if (!isGetAllFilesCompleted)
            {
                break;
            }

            alreadyCompletedDirectories.Add(currentSearchDirectory);
        }

        _logger.LogInformation(isGetAllFilesCompleted 
                                   ? $"Finished iterative search for finding files from '{directoryToBackup}'" 
                                   : $"Paused iterative search in '{directoryToBackup}'");
        return new FilesBackupData
        {
            IsGetAllFilesCompleted = isGetAllFilesCompleted,
            AlreadyBackedUpFilePathToFileHashMap = alreadyBackedUpFilePathToFileHashMap,
            NotBackedUpFilePathToFileHashMap = notBackedUpFilePathToFileHashMap
        };
    }

    private static async Task UpdateLastBackupTime(string? backupDescription)
    {
        await File.AppendAllTextAsync(Consts.Data.BackupTimeDiaryFilePath, $"{DateTime.Now} --- {backupDescription}" + Environment.NewLine);
    }

    private void WarnForNonBackupFiles()
    {
        foreach(string directory in Directory.EnumerateDirectories(Consts.Data.ReadyToBackupDirectoryPath))
        {
            if (Directory.GetFiles(directory, "*", SearchOption.AllDirectories).Length > 0)
            {
                _logger.LogWarning("Not all files were backuped!!! Found non mapped directory '{directory}'. Consider use CLI command 'UpdateSettingsHandler'", directory);
            }
        }
    }

    private async Task<bool> AddFilesToBackupOnlyIfFileNotBackedUpAlready(IDictionary<FileSystemPath, string> notBackedUpFilePathToFileHashMap,
                                                                          IDictionary<FileSystemPath, string> alreadyBackedUpFilePathToFileHashMap,
                                                                          string directory,
                                                                          string sourceRelativeDirectory,
                                                                          BackupSettings backupSettings,
                                                                          CancellationToken cancellationToken)
    {
        bool isGetAllFilesCompleted = false;
        foreach (string filePath in EnumerateFiles(directory))
        {
            FileSystemPath fileSystemPath = new(filePath);
            FileSystemPath sourceRelativeFilePath = fileSystemPath.GetRelativePath(sourceRelativeDirectory);
            
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Cancel requested");
                break;
            }
            
            (string? fileHash, bool isAlreadyBackedUp) = await GetFileHashData(filePath, sourceRelativeFilePath.PathString, backupSettings.SearchMethod, cancellationToken).ConfigureAwait(false);
            
            if (isAlreadyBackedUp)
            {
                _logger.LogTrace($"File '{fileSystemPath}' already backed up");

                if (!string.IsNullOrWhiteSpace(fileHash))
                {
                    alreadyBackedUpFilePathToFileHashMap.Add(fileSystemPath, fileHash);
                }
                
                continue;
            }
            
            if (string.IsNullOrWhiteSpace(fileHash))
            {
                _logger.LogError($"Hash for '{filePath}' was not calculated");
                continue;
            }

            notBackedUpFilePathToFileHashMap.Add(fileSystemPath, fileHash);
            _logger.LogInformation($"Found file '{fileSystemPath}' to backup");
            
            if (notBackedUpFilePathToFileHashMap.Count >= backupSettings.SaveInterval)
            {
                _logger.LogInformation($"Collected {notBackedUpFilePathToFileHashMap.Count} files, pausing file fetch");
                return isGetAllFilesCompleted;
            }
        }

        _logger.LogInformation($"Collected {notBackedUpFilePathToFileHashMap.Count} files");
        isGetAllFilesCompleted = true;
        return isGetAllFilesCompleted;
    }
    
    private async Task BackupFilesInternal(BackupSettings backupSettings,
                                           Dictionary<FileSystemPath, string> filePathToBackupToFileHashMap,
                                           DirectoriesMap directoriesMap,
                                           CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            $"Copying {filePathToBackupToFileHashMap.Count} files from '{directoriesMap.SourceRelativeDirectory}' to '{directoriesMap.DestRelativeDirectory}'");

        FileSystemPath destinationDirectoryPath = BuildDestinationDirectoryPath(backupSettings);

        ushort backupFilesIntervalCount = 0;
        foreach ((FileSystemPath fileToBackup, string fileHash) in filePathToBackupToFileHashMap)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation($"Cancel requested");
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
                _logger.LogInformation("Copied '{fileToBackup}' to '{destinationFilePath}'", fileToBackup, destinationFilePath);
                
                await _filesHashesHandler.AddFileHash(fileHash, relativeFilePathToBackup.PathString, cancellationToken).ConfigureAwait(false);

                backupFilesIntervalCount++;
                if (backupFilesIntervalCount % backupSettings.SaveInterval == 0)
                {
                    _logger.LogDebug($"Reached save interval {backupFilesIntervalCount}");
                    await _filesHashesHandler.Save(cancellationToken).ConfigureAwait(false);
                    backupFilesIntervalCount = 0;
                }

                if (!backupSettings.ShouldBackupToKnownDirectory)
                {
                    _ = tryMoveFileFromReadyToBackupDirectoryToBackedUpDirectory(fileToBackup);
                }
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, $"Failed to copy '{fileToBackup}' to '{destinationFilePath}'");
            }
        }
        
        _logger.LogInformation($"Done copy {filePathToBackupToFileHashMap.Count} files from '{directoriesMap.SourceRelativeDirectory}' to '{directoriesMap.DestRelativeDirectory}'");
        await _filesHashesHandler.Save(cancellationToken).ConfigureAwait(false);
    }

    private void MoveFilesToBackedUpDirectoryIfRequired(BackupSettings backupSettings,
                                                        Dictionary<FileSystemPath, string> alreadyBackedUpFilePathToFileHashMap)
    {
        if (backupSettings.ShouldBackupToKnownDirectory)
        {
            return;
        }
        
        foreach ((FileSystemPath fileToBackup, string _) in alreadyBackedUpFilePathToFileHashMap)
        {
            _ = tryMoveFileFromReadyToBackupDirectoryToBackedUpDirectory(fileToBackup);
        }
    }

    private static FileSystemPath BuildDestinationDirectoryPath(BackupSettings backupSettings)
    {
        if (backupSettings.ShouldBackupToKnownDirectory)
        {
            return new FileSystemPath(Consts.Data.WaitingApprovalDirectoryPath);
        }

        if (string.IsNullOrWhiteSpace(backupSettings.RootDirectory))
        {
            throw new NullReferenceException(
                $"{nameof(backupSettings.RootDirectory)} is empty while {nameof(backupSettings.ShouldBackupToKnownDirectory)} is {backupSettings.ShouldBackupToKnownDirectory}");
        }

        return new FileSystemPath(backupSettings.RootDirectory);
    }
    
    private static FileSystemPath BuildDestinationFilePath(FileSystemPath relativeSourceFilePath,
                                                           FileSystemPath destinationDirectoryPath,
                                                           DirectoriesMap directoriesMap)
    {
        FileSystemPath relativeDestinationFilePath = BuildRelativeDestinationFilePath(relativeSourceFilePath, destinationDirectoryPath, directoriesMap);
        return destinationDirectoryPath.Combine(relativeDestinationFilePath);
    }

    private static FileSystemPath BuildRelativeDestinationFilePath(FileSystemPath relativeSourceFilePath,
                                                                   FileSystemPath destinationDirectoryPath,
                                                                   DirectoriesMap directoriesMap)
    {
        if (string.IsNullOrWhiteSpace(directoriesMap.DestRelativeDirectory))
        {
            return relativeSourceFilePath;
        }
        
        FileSystemPath relativeDestinationFilePath = relativeSourceFilePath.Replace(
            directoriesMap.SourceRelativeDirectory, directoriesMap.DestRelativeDirectory);

        if (!relativeDestinationFilePath.PathString.Contains(directoriesMap.DestRelativeDirectory))
        {
            FileSystemPath destRelativeDirectory = new(directoriesMap.DestRelativeDirectory);
            FileSystemPath relativeDestinationDirectoryPath = destRelativeDirectory.GetRelativePath(destinationDirectoryPath.PathString);
            relativeDestinationFilePath = relativeDestinationDirectoryPath.Combine(relativeDestinationFilePath);
        }
        
        return relativeDestinationFilePath;
    }

    private static FileSystemPath BuildRelativeSourceFilePath(FileSystemPath sourceFilePath, BackupSettings backupSettings)
    {
        if (backupSettings.ShouldBackupToKnownDirectory)
        {
            return string.IsNullOrWhiteSpace(backupSettings.RootDirectory) 
                ? sourceFilePath
                : sourceFilePath.GetRelativePath(backupSettings.RootDirectory);
        }
        
        return sourceFilePath.GetRelativePath(Consts.Data.ReadyToBackupDirectoryPath);
    }

    private bool tryMoveFileFromReadyToBackupDirectoryToBackedUpDirectory(FileSystemPath readyToBackupFilePath)
    {
        try
        {
            FileSystemPath backedUpFilePath = readyToBackupFilePath.Replace(Consts.Data.ReadyToBackupDirectoryPath,
                                                                            Consts.Data.BackedUpDirectoryPath);

            string parentDirectory = Path.GetDirectoryName(backedUpFilePath.PathString)
                ?? throw new NullReferenceException($"Directory of '{backedUpFilePath.PathString}' is empty");
            _ = Directory.CreateDirectory(parentDirectory);
            _logger.LogTrace($"Moving {readyToBackupFilePath.PathString} to {Consts.Data.BackedUpDirectoryPath}");
            File.Move(readyToBackupFilePath.PathString, backedUpFilePath.PathString);
            return true;
        }
        catch (Exception ex)
        {
           _logger.LogError(ex, $"Failed to move {readyToBackupFilePath.PathString} to {Consts.Data.BackedUpDirectoryPath}");
           return false;
        }
    }
}
