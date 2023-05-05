﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.Domain.Hash;
using BackupManager.Domain.Mapping;
using BackupManager.Domain.Settings;
using BackupManager.Infra;
using BackupManager.Infra.Backup.Services;
using JsonSerialization;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BackupManagerTests.Infra.Backup.Services;

public class MediaDeviceBackupServiceTests : TestsBase
{
    [Fact(Skip = "Requires real device connected")]
    // [Fact]
    public async Task BackupFiles_FilesAndDirectoriesToBackup_FilesAndDirectoriesAreBackuped()
    {
        JsonSerializer jsonSerializer = new();

        BackupSettings backupSettings = new()
        {
            RootDirectory = "Internal shared storage",
            DirectoriesSourcesToDirectoriesDestinationMap = new List<DirectoriesMap>
            {
                new()
                {
                    SourceRelativeDirectory = "DCIM/Screenshots_tests",
                    DestRelativeDirectory = "Screenshots"
                }
            }
        };
        
        FilesHashesHandler filesHashesHandler = new(jsonSerializer, NullLogger<FilesHashesHandler>.Instance);

        MediaDeviceBackupService backupService = new("Redmi Note 8 Pro", filesHashesHandler, NullLoggerFactory.Instance);

        await backupService.BackupFiles(backupSettings, CancellationToken.None).ConfigureAwait(false);

        Assert.True(File.Exists(Path.Combine(Consts.BackupsDirectoryPath, "Screenshots", "Screenshot_2020-03-12-20-42-59-175_com.facebook.katana.jpg")));
        Assert.True(File.Exists(Path.Combine(Consts.BackupsDirectoryPath, "Screenshots", "another dir", "Screenshot_same_copy.jpg")));

        string lastBackupTimeStr = (await File.ReadAllLinesAsync(Consts.BackupTimeDiaryFilePath).ConfigureAwait(false))[^1];
        DateTime lastBackupTime = DateTime.Parse(lastBackupTimeStr);
        Assert.Equal(DateTime.Now.Date, lastBackupTime.Date);

        Dictionary<string, List<string>> hashesToFilePath = jsonSerializer.Deserialize<Dictionary<string, List<string>>>(Consts.DataFilePath);
        Assert.Equal(Path.Combine("\\DCIM", "Screenshots_tests", "Screenshot_2020-03-12-20-42-59-175_com.facebook.katana.jpg"), hashesToFilePath["2C913FF054E9A626ED7D49A6B26CC9CE912AC39DA0E1EFD5A3077988955B97C6"][0]);
    }
    
    [Fact(Skip = "Requires real device connected")]
    // [Fact]
    public async Task BackupFiles_FilesAndDirectoriesToBackup_EmptyDestRelativeDirectory_FilesAndDirectoriesAreBackupedIntoBackupDirectoryWithPreservingStructure()
    {
        JsonSerializer jsonSerializer = new();

        BackupSettings backupSettings = new()
        {
            RootDirectory = "Internal shared storage",
            DirectoriesSourcesToDirectoriesDestinationMap = new List<DirectoriesMap>
            {
                new()
                {
                    SourceRelativeDirectory = "DCIM/Screenshots_tests",
                    DestRelativeDirectory = ""
                }
            }
        };
        
        FilesHashesHandler filesHashesHandler = new(jsonSerializer, NullLogger<FilesHashesHandler>.Instance);

        MediaDeviceBackupService backupService = new("Redmi Note 8 Pro", filesHashesHandler, NullLoggerFactory.Instance);

        await backupService.BackupFiles(backupSettings, CancellationToken.None).ConfigureAwait(false);

        Assert.True(File.Exists(Path.Combine(Consts.BackupsDirectoryPath, "DCIM", "Screenshots_tests", "Screenshot_2020-03-12-20-42-59-175_com.facebook.katana.jpg")));
        Assert.True(File.Exists(Path.Combine(Consts.BackupsDirectoryPath, "DCIM", "Screenshots_tests", "another dir", "Screenshot_same_copy.jpg")));

        string lastBackupTimeStr = (await File.ReadAllLinesAsync(Consts.BackupTimeDiaryFilePath).ConfigureAwait(false))[^1];
        DateTime lastBackupTime = DateTime.Parse(lastBackupTimeStr);
        Assert.Equal(DateTime.Now.Date, lastBackupTime.Date);

        Dictionary<string, List<string>> hashesToFilePath = jsonSerializer.Deserialize<Dictionary<string, List<string>>>(Consts.DataFilePath);
        Assert.Equal(Path.Combine("\\DCIM","Screenshots_tests", "Screenshot_2020-03-12-20-42-59-175_com.facebook.katana.jpg"), hashesToFilePath["2C913FF054E9A626ED7D49A6B26CC9CE912AC39DA0E1EFD5A3077988955B97C6"][0]);
    }
    
    [Fact(Skip = "Requires real device connected")]
    // [Fact]
    public async Task BackupFiles_FilesAndDirectoriesToBackup_SourceRelativeDirectoryIsOneLevelOnly_EmptyDestRelativeDirectory_FilesAndDirectoriesAreBackupedIntoBackupDirectoryWithPreservingStructure()
    {
        JsonSerializer jsonSerializer = new();

        BackupSettings backupSettings = new()
        {
            RootDirectory = "Internal shared storage",
            DirectoriesSourcesToDirectoriesDestinationMap = new List<DirectoriesMap>
            {
                new()
                {
                    SourceRelativeDirectory = "TestDir",
                    DestRelativeDirectory = ""
                }
            }
        };
        
        FilesHashesHandler filesHashesHandler = new(jsonSerializer, NullLogger<FilesHashesHandler>.Instance);

        MediaDeviceBackupService backupService = new("Redmi Note 8 Pro", filesHashesHandler, NullLoggerFactory.Instance);

        await backupService.BackupFiles(backupSettings, CancellationToken.None).ConfigureAwait(false);

        Assert.True(File.Exists(Path.Combine(Consts.BackupsDirectoryPath, "TestDir", "deviceId.txt")));

        string lastBackupTimeStr = (await File.ReadAllLinesAsync(Consts.BackupTimeDiaryFilePath).ConfigureAwait(false))[^1];
        DateTime lastBackupTime = DateTime.Parse(lastBackupTimeStr);
        Assert.Equal(DateTime.Now.Date, lastBackupTime.Date);

        Dictionary<string, List<string>> hashesToFilePath = jsonSerializer.Deserialize<Dictionary<string, List<string>>>(Consts.DataFilePath);
        Assert.Equal(Path.Combine("\\TestDir","deviceId.txt"), hashesToFilePath["726B219710AB5B7155C93F8E1854849BF48EAD801A97CB546B69E5BC2E7DC12F"][0]);
    }
}