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
    public DriveBackupService(IFilesHashesHandler filesHashesHandler, ILoggerFactory loggerFactory)
        : base(filesHashesHandler, loggerFactory)
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

    protected override async Task<(string?, bool)> GetFileHashData(string filePath, string relativeFilePath, SearchMethod searchMethod, CancellationToken cancellationToken)
    {
        string fileHash = _filesHashesHandler.CalculateHash(filePath);
        return searchMethod switch
        {
            SearchMethod.Hash     => (fileHash, await _filesHashesHandler.IsHashExists(fileHash, cancellationToken).ConfigureAwait(false)),
            SearchMethod.FilePath => (fileHash, await _filesHashesHandler.IsFilePathExist(relativeFilePath, cancellationToken).ConfigureAwait(false)),
            _                     => throw new NotSupportedException($"Search method {searchMethod} not supported at the moment")
        };
    }

    protected override void CopyFile(string fileToBackup, string destinationFilePath)
    {
        if (!File.Exists(destinationFilePath))
        {
            File.Copy(fileToBackup, destinationFilePath);
            return;
        }

        CopyFileWithAutoRename(fileToBackup, destinationFilePath);
    }

    private static void CopyFileWithAutoRename(string fileToBackup, string destinationFilePath)
    {
        string directory = Path.GetDirectoryName(destinationFilePath) ?? throw new ArgumentNullException(nameof(destinationFilePath));
        string filename = Path.GetFileNameWithoutExtension(destinationFilePath);
        string extension = Path.GetExtension(destinationFilePath);

        string finalPath = destinationFilePath;
        int i = 1;

        // Try suffixes until we find a free filename
        while (File.Exists(finalPath))
        {
            finalPath = Path.Combine(directory, $"{filename}_{i}{extension}");
            i++;
        }

        File.Copy(fileToBackup, finalPath);
    }

    protected override bool IsDirectoryExists(string directory)
    {
        return Directory.Exists(directory);
    }
}