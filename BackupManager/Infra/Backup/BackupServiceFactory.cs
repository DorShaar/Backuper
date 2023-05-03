﻿using System;
using BackupManager.App;
using BackupManager.Domain.Enums;
using BackupManager.Domain.Hash;
using BackupManager.Domain.Settings;
using BackupManager.Infra.Backup.Services;
using Microsoft.Extensions.Logging;

namespace BackupManager.Infra.Backup;

public class BackupServiceFactory
{
    private readonly FilesHashesHandler mFilesHashesHandler;
    private readonly DriveBackupService mDriveBackupService;
    private readonly ILoggerFactory mLoggerFactory;
    
    private MediaDeviceBackupService? mMediaDeviceBackupService;

    public BackupServiceFactory(DriveBackupService driveBackupService,
        FilesHashesHandler filesHashesHandler,
        ILoggerFactory loggerFactory)
    {
        mFilesHashesHandler = filesHashesHandler;
        mDriveBackupService = driveBackupService;
        mLoggerFactory = loggerFactory;
    }

    public IBackupService Create(BackupSettings backupSettings)
    {
        return backupSettings.SourceType switch
        {
            SourceType.MediaDevice => createMediaDeviceBackupServiceAndAddAsMember(backupSettings.MediaDeviceName),
            SourceType.DriveOrDirectory => mDriveBackupService,
            _ => throw new NotSupportedException($"Source type {backupSettings.SourceType} is not supported at the moment")
        };
    }

    private IBackupService createMediaDeviceBackupServiceAndAddAsMember(string? mediaDeviceName)
    {
        if (string.IsNullOrWhiteSpace(mediaDeviceName))
        {
            throw new ArgumentException($"Invalid device name '{mediaDeviceName}'");
        }
        
        mMediaDeviceBackupService = new MediaDeviceBackupService(mediaDeviceName, mFilesHashesHandler, mLoggerFactory.CreateLogger<MediaDeviceBackupService>());
        return mMediaDeviceBackupService;
    }
}