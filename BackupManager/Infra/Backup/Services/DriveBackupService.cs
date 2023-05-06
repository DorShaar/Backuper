using System;
using System.Collections.Generic;
using System.IO;
using BackupManager.Domain.Enums;
using BackupManager.Domain.Hash;
using Microsoft.Extensions.Logging;

namespace BackupManager.Infra.Backup.Services;

public class DriveBackupService : BackupServiceBase
{
    public DriveBackupService(IFilesHashesHandler filesHashesHandler, ILoggerFactory loggerFactory) : base(filesHashesHandler, loggerFactory)
    {
    }

    protected override void AddDirectoriesToSearchQueue(Queue<string> directoriesToSearch, string currentSearchDirectory)
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

    protected override (string fileHash, bool isAlreadyBackuped) GetFileHashData(string filePath, string relativeFilePath, SearchMethod searchMethod)
    {
        string fileHash = mFilesHashesHandler.CalculateHash(filePath);
        return searchMethod switch
        {
            SearchMethod.Hash => (fileHash, mFilesHashesHandler.IsHashExists(fileHash)),
            SearchMethod.FilePath => (fileHash, mFilesHashesHandler.IsFilePathExist(relativeFilePath)),
            _ => throw new NotSupportedException($"Search method {searchMethod} not supported at the moment")
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