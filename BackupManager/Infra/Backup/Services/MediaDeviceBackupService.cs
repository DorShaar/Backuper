﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.App.Database;
using BackupManager.Domain.Enums;
using BackupManagerCore;
using MediaDevices;
using Microsoft.Extensions.Logging;
using Temporaries;

namespace BackupManager.Infra.Backup.Services;

#pragma warning disable CA1416
public class MediaDeviceBackupService : BackupServiceBase
{
    private readonly MediaDevice mMediaDevice;
    
    public MediaDeviceBackupService(string deviceName,
        IFilesHashesHandler database,
        ILoggerFactory loggerFactory) : base(database, loggerFactory)
    {
        mMediaDevice = MediaDevice.GetDevices().First(device => device.Description == deviceName);
        mMediaDevice.Connect();
    }

    public override void Dispose()
    {
        _logger.LogInformation($"Closing media device {mMediaDevice.Description}");
        GC.SuppressFinalize(this);
        mMediaDevice.Dispose();
    }

    protected override void AddSubDirectoriesToSearchQueue(Queue<string> directoriesToSearch, string currentSearchDirectory)
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

    protected override async Task<(string?, bool)> GetFileHashData(string filePath, string relativeFilePath, SearchMethod searchMethod, CancellationToken cancellationToken)
    {
        bool isAlreadyBackedUp;
        
        if (searchMethod == SearchMethod.FilePath)
        {
            isAlreadyBackedUp = await _filesHashesHandler.IsFilePathExist(relativeFilePath, cancellationToken).ConfigureAwait(false);

            if (isAlreadyBackedUp)
            {
                // Since found as already backed up, calculating hash again is not required. 
                return (null, true);
            }
        }
        
        _ = Directory.CreateDirectory(Consts.TempDirectoryPath);
        
        string tempFilePath = Path.Combine(Consts.TempDirectoryPath, Path.GetRandomFileName());
        using TempFile tempFile = new(tempFilePath);
        CopyFile(filePath, tempFile.Path);
        string fileHash = _filesHashesHandler.CalculateHash(tempFile.Path);

        isAlreadyBackedUp = false;
        if (searchMethod == SearchMethod.Hash)
        {
            isAlreadyBackedUp = await _filesHashesHandler.IsHashExists(fileHash, cancellationToken).ConfigureAwait(false);
        }
        
        return (fileHash, isAlreadyBackedUp);
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
            _logger.LogError(ex, $"Could not get directory info for {directory}");
            return false;
        }
    }
}
#pragma warning restore CA1416
