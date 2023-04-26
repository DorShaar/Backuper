﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaDevices;
using Microsoft.Extensions.Logging;
using Temporaries;

namespace BackupManager.Infra.Backup;

#pragma warning disable CA1416
// TOdO DOR call in factory.
public class MediaDeviceBackupService : BackupServiceBase
{
    private readonly MediaDevice mMediaDevice;
    
    public MediaDeviceBackupService(string deviceName,
        FilesHashesHandler filesHashesHandler,
        ILogger<BackupServiceBase> logger) : base(filesHashesHandler, logger)
    {
        mMediaDevice = MediaDevice.GetDevices().First(device => device.FriendlyName == deviceName);
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

    protected override (string fileHash, bool isFileHashExist) GetFileHashData(string filePath)
    {
        string tempFilePath = Path.Combine(Consts.TempDirectoryPath, Path.GetRandomFileName());
        using TempFile tempFile = new(tempFilePath);
        CopyFile(filePath, tempFile.Path);
        return mFilesHashesHandler.IsFileHashExist(tempFile.Path);
    }

    protected override void CopyFile(string fileToBackup, string destinationFilePath)
    {
        MediaFileInfo mediaFileInfo = mMediaDevice.GetFileInfo(fileToBackup);
        mediaFileInfo.CopyTo(destinationFilePath);
    }
}
#pragma warning restore CA1416