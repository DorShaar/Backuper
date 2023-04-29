using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BackupManager.Domain.Enums;
using BackupManager.Domain.Hash;
using MediaDevices;
using Microsoft.Extensions.Logging;
using Temporaries;

namespace BackupManager.Infra.Backup.Services;

#pragma warning disable CA1416
public class MediaDeviceBackupService : BackupServiceBase
{
    private readonly MediaDevice mMediaDevice;
    
    public MediaDeviceBackupService(string deviceName,
        FilesHashesHandler filesHashesHandler,
        ILogger<BackupServiceBase> logger) : base(filesHashesHandler, logger)
    {
        mMediaDevice = MediaDevice.GetDevices().First(device => device.FriendlyName == deviceName);
        mMediaDevice.Connect();
    }

    public override void Dispose()
    {
        GC.SuppressFinalize(this);
        mMediaDevice.Dispose();
    }

    protected override void AddDirectoriesToSearchQueue(Queue<string> directoriesToSearch, string currentSearchDirectory)
    {
        MediaDirectoryInfo currentSearchMediaDirectory = mMediaDevice.GetDirectoryInfo(currentSearchDirectory);
        foreach (MediaDirectoryInfo subDirectory in currentSearchMediaDirectory.EnumerateDirectories())
        {
            directoriesToSearch.Enqueue(subDirectory.FullName);
        }
    }

    protected override IEnumerable<string> EnumerateFiles(string directory)
    {
        MediaDirectoryInfo mediaDirectoryInfo = mMediaDevice.GetDirectoryInfo(directory);
        return mediaDirectoryInfo.EnumerateFiles().Select(mediaFileInfo => mediaFileInfo.FullName);
    }

    protected override (string? fileHash, bool isAlreadyBackuped) GetFileHashData(string filePath, SearchMethod searchMethod)
    {
        bool isAlreadyBackuped = mFilesHashesHandler.IsFilePathExist(filePath);

        if (isAlreadyBackuped)
        {
            // Since found as already backuped, calculating hash again is not required. 
            return (null, true);
        }
        
        _ = Directory.CreateDirectory(Consts.TempDirectoryPath);
        
        string tempFilePath = Path.Combine(Consts.TempDirectoryPath, Path.GetRandomFileName());
        using TempFile tempFile = new(tempFilePath);
        CopyFile(filePath, tempFile.Path);
        string fileHash = mFilesHashesHandler.CalculateHash(tempFile.Path);

        return (fileHash, false);
    }

    protected override void CopyFile(string fileToBackup, string destinationFilePath)
    {
        MediaFileInfo mediaFileInfo = mMediaDevice.GetFileInfo(fileToBackup);
        mediaFileInfo.CopyTo(destinationFilePath);
    }

    protected override bool IsDirectoryExists(string directory)
    {
        try
        {
            _ = mMediaDevice.GetDirectoryInfo(directory);
            return true;
        }
        catch (Exception ex)
        {
            mLogger.LogError(ex, $"Could not get directory info for {directory}");
            return false;
        }
    }
}
#pragma warning restore CA1416
