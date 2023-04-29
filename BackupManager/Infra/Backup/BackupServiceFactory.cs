using System;
using BackupManager.App;
using BackupManager.Domain.Enums;
using BackupManager.Infra.Backup.Services;

namespace BackupManager.Infra.Backup;

public class BackupServiceFactory
{
    private readonly DriveBackupService mDriveBackupService;
    private readonly MediaDeviceBackupService mMediaDeviceBackupService;

    public BackupServiceFactory(DriveBackupService driveBackupService, MediaDeviceBackupService mediaDeviceBackupService)
    {
        mDriveBackupService = driveBackupService;
        mMediaDeviceBackupService = mediaDeviceBackupService;
    }

    public IBackupService Create(SourceType sourceType)
    {
        return sourceType switch
        {
            SourceType.MediaDevice => mMediaDeviceBackupService,
            SourceType.DriveOrDirectory => mDriveBackupService,
            _ => throw new NotSupportedException($"Source type {sourceType} is not supported at the moment")
        };
    }
}