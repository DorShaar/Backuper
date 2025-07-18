﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.App.Database;
using BackupManager.Domain.Settings;
using BackupManager.Infra.Backup.Services;
using BackupManager.Infra.DB.LocalJsonFileDatabase;
using BackupManager.Infra.FileHashHandlers;
using BackupManagerCore;
using BackupManagerCore.Mapping;
using BackupManagerCore.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BackupManagerTests.Infra.Backup.Services;

public class MediaDeviceBackupServiceTests : TestsBase
{
    [Fact(Skip = "Requires real device connected")]
    // [Fact]
    public async Task BackupFiles_FilesAndDirectoriesToBackup_FilesAndDirectoriesAreBackuped()
    {
        BackupSerializedSettings backupSerializedSettings = new()
        {
            IsFromInstallation = true,
            DirectoriesSourcesToDirectoriesDestinationMap = new List<DirectoriesMap>
            {
                new()
                {
                    SourceRelativeDirectory = "DCIM/Screenshots_tests",
                    DestRelativeDirectory = "Screenshots"
                }
            }
        };

        BackupSettings backupSettings = new(backupSerializedSettings, "Internal shared storage");
        
        LocalJsonDatabase localJsonDatabase = new(mJsonSerializer, NullLogger<LocalJsonDatabase>.Instance);
        FilesHashesHandler filesHashesHandler = new(new List<IBackedUpFilesDatabase> {localJsonDatabase});

        MediaDeviceBackupService backupService = new("Redmi Note 8 Pro", filesHashesHandler, NullLoggerFactory.Instance);

        await backupService.BackupFiles(backupSettings, CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(Consts.Data.WaitingApprovalDirectoryPath, "Screenshots", "Screenshot_2020-03-12-20-42-59-175_com.facebook.katana.jpg")));
        Assert.True(File.Exists(Path.Combine(Consts.Data.WaitingApprovalDirectoryPath, "Screenshots", "another dir", "Screenshot_same_copy.jpg")));

        string lastBackupTimeStr = (await File.ReadAllLinesAsync(Consts.Data.BackupTimeDiaryFilePath))[^1];
        DateTime lastBackupTime = DateTime.Parse(lastBackupTimeStr);
        Assert.Equal(DateTime.Now.Date, lastBackupTime.Date);

        Dictionary<string, List<string>> hashesToFilePath =
            await mJsonSerializer.DeserializeAsync<Dictionary<string, List<string>>>(GetBackedUpFilesLocalFilePath(), CancellationToken.None);
        Assert.Equal(Path.Combine("\\DCIM", "Screenshots_tests", "Screenshot_2020-03-12-20-42-59-175_com.facebook.katana.jpg"), hashesToFilePath["2C913FF054E9A626ED7D49A6B26CC9CE912AC39DA0E1EFD5A3077988955B97C6"][0]);
    }
    
    [Fact(Skip = "Requires real device connected")]
    // [Fact]
    public async Task BackupFiles_FilesAndDirectoriesToBackup_EmptyDestRelativeDirectory_FilesAndDirectoriesAreBackupedIntoBackupDirectoryWithPreservingStructure()
    {
        BackupSerializedSettings backupSerializedSettings = new()
        {
            IsFromInstallation = true,
            DirectoriesSourcesToDirectoriesDestinationMap = new List<DirectoriesMap>
            {
                new()
                {
                    SourceRelativeDirectory = "DCIM/Screenshots_tests",
                    DestRelativeDirectory = ""
                }
            }
        };

        BackupSettings backupSettings = new(backupSerializedSettings, "Internal shared storage");
        
        LocalJsonDatabase localJsonDatabase = new(mJsonSerializer, NullLogger<LocalJsonDatabase>.Instance);
        FilesHashesHandler filesHashesHandler = new(new List<IBackedUpFilesDatabase> {localJsonDatabase});

        MediaDeviceBackupService backupService = new("Redmi Note 8 Pro", filesHashesHandler, NullLoggerFactory.Instance);

        await backupService.BackupFiles(backupSettings, CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(Consts.Data.WaitingApprovalDirectoryPath, "DCIM", "Screenshots_tests", "Screenshot_2020-03-12-20-42-59-175_com.facebook.katana.jpg")));
        Assert.True(File.Exists(Path.Combine(Consts.Data.WaitingApprovalDirectoryPath, "DCIM", "Screenshots_tests", "another dir", "Screenshot_same_copy.jpg")));

        string lastBackupTimeStr = (await File.ReadAllLinesAsync(Consts.Data.BackupTimeDiaryFilePath))[^1];
        DateTime lastBackupTime = DateTime.Parse(lastBackupTimeStr);
        Assert.Equal(DateTime.Now.Date, lastBackupTime.Date);

        Dictionary<string, List<string>> hashesToFilePath =
            await mJsonSerializer.DeserializeAsync<Dictionary<string, List<string>>>(GetBackedUpFilesLocalFilePath(), CancellationToken.None);
        Assert.Equal(Path.Combine("\\DCIM","Screenshots_tests", "Screenshot_2020-03-12-20-42-59-175_com.facebook.katana.jpg"), hashesToFilePath["2C913FF054E9A626ED7D49A6B26CC9CE912AC39DA0E1EFD5A3077988955B97C6"][0]);
    }
    
    [Fact(Skip = "Requires real device connected")]
    // [Fact]
    public async Task BackupFiles_FilesAndDirectoriesToBackup_SourceRelativeDirectoryIsOneLevelOnly_EmptyDestRelativeDirectory_FilesAndDirectoriesAreBackupedIntoBackupDirectoryWithPreservingStructure()
    {
        BackupSerializedSettings backupSerializedSettings = new()
        {
            IsFromInstallation = true,
            DirectoriesSourcesToDirectoriesDestinationMap = new List<DirectoriesMap>
            {
                new()
                {
                    SourceRelativeDirectory = "TestDir",
                    DestRelativeDirectory = ""
                }
            }
        };
        
        BackupSettings backupSettings = new(backupSerializedSettings, "Internal shared storage");
        
        LocalJsonDatabase localJsonDatabase = new(mJsonSerializer, NullLogger<LocalJsonDatabase>.Instance);
        FilesHashesHandler filesHashesHandler = new(new List<IBackedUpFilesDatabase> {localJsonDatabase});

        MediaDeviceBackupService backupService = new("Redmi Note 8 Pro", filesHashesHandler, NullLoggerFactory.Instance);

        await backupService.BackupFiles(backupSettings, CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(Consts.Data.WaitingApprovalDirectoryPath, "TestDir", "deviceId.txt")));

        string lastBackupTimeStr = (await File.ReadAllLinesAsync(Consts.Data.BackupTimeDiaryFilePath))[^1];
        DateTime lastBackupTime = DateTime.Parse(lastBackupTimeStr);
        Assert.Equal(DateTime.Now.Date, lastBackupTime.Date);

        Dictionary<string, List<string>> hashesToFilePath =
            await mJsonSerializer.DeserializeAsync<Dictionary<string, List<string>>>(GetBackedUpFilesLocalFilePath(), CancellationToken.None);
        Assert.Equal(Path.Combine("\\TestDir","deviceId.txt"), hashesToFilePath["726B219710AB5B7155C93F8E1854849BF48EAD801A97CB546B69E5BC2E7DC12F"][0]);
    }
}