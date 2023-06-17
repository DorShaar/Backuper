using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.App.Database;
using BackupManager.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BackupManager.Infra.Backup.Services;

public class DriveBackupService : BackupServiceBase
{
    public DriveBackupService(IFilesHashesHandler filesHashesHandler, ILoggerFactory loggerFactory) : base(filesHashesHandler, loggerFactory)
    {
    }

    protected override void AddSubDirectoriesToSearchQueue(Queue<string> directoriesToSearch, string currentSearchDirectory)
    {
        foreach (string directory in Directory.EnumerateDirectories(currentSearchDirectory))
        {
            directoriesToSearch.Enqueue(directory);
        }
    }

    protected override IEnumerable<string> EnumerateFiles(string directory)
    {
        return Directory.EnumerateFiles(directory);
    }

    protected override async Task<(string? fileHash, bool isAlreadyBackuped)> GetFileHashData(string filePath, string relativeFilePath, SearchMethod searchMethod, CancellationToken cancellationToken)
    {
        string fileHash = mFilesHashesHandler.CalculateHash(filePath);
        return searchMethod switch
        {
            SearchMethod.Hash     => (fileHash, await mFilesHashesHandler.IsHashExists(fileHash, cancellationToken).ConfigureAwait(false)),
            SearchMethod.FilePath => (fileHash, await mFilesHashesHandler.IsFilePathExist(relativeFilePath, cancellationToken).ConfigureAwait(false)),
            _                     => throw new NotSupportedException($"Search method {searchMethod} not supported at the moment")
        };
    }

    protected override void CopyFile(string fileToBackup, string destinationFilePath)
    {
        File.Copy(fileToBackup, destinationFilePath, overwrite: true);
    }

    protected override bool IsDirectoryExists(string directory)
    {
        return Directory.Exists(directory);
    }
}