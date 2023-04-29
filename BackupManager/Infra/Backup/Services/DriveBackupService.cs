using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;

namespace BackupManager.Infra.Backup.Services;

public class DriveBackupService : BackupServiceBase
{
    public DriveBackupService(FilesHashesHandler filesHashesHandler, ILogger<BackupServiceBase> logger) : base(filesHashesHandler, logger)
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

    protected override (string fileHash, bool isFileHashExist) GetFileHashData(string filePath)
    {
        return mFilesHashesHandler.IsFileHashExist(filePath);
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