using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using BackupManager.Domain.Mapping;
using BackupManager.Domain.Settings;
using BackupManager.Infra;
using BackupManager.Infra.Backup.Services;
using BackupManager.Infra.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BackupManagerTests.Infra.Backup.Services;

public class MediaDeviceBackupServiceTests : TestsBase
{
    [Fact(Skip = "Requires real device connected")]
    // [Fact]
    public void BackupFiles_FilesAndDirectoriesToBackup_FilesAndDirectoriesAreBackuped()
    {
        JsonSerializerWrapper objectSerializer = new();

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
        
        FilesHashesHandler filesHashesHandler = new(objectSerializer, NullLogger<FilesHashesHandler>.Instance);

        MediaDeviceBackupService backupService = new("Redmi Note 8 Pro", filesHashesHandler, NullLogger<DriveBackupService>.Instance);

        backupService.BackupFiles(backupSettings, CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(Consts.BackupsDirectoryPath, "Screenshots" ,"Screenshot_2020-03-12-20-42-59-175_com.facebook.katana.jpg")));

        string lastBackupTimeStr = File.ReadAllLines(Consts.BackupTimeDiaryFilePath)[^1];
        DateTime lastBackupTime = DateTime.Parse(lastBackupTimeStr);
        Assert.Equal(DateTime.Now.Date, lastBackupTime.Date);

        Dictionary<string, List<string>> hashesToFilePath = objectSerializer.Deserialize<Dictionary<string, List<string>>>(Consts.DataFilePath);
        Assert.Equal("DCIM/Screenshots_tests/Screenshot_2020-03-12-20-42-59-175_com.facebook.katana.jpg", hashesToFilePath["5B0CCEF73B8DCF768B3EBCFBB902269389C0224202F120C1AA25137AC2C27551"][0]);
    }
}