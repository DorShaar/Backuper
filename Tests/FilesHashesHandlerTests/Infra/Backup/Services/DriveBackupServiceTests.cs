using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using BackupManager.Domain.Hash;
using BackupManager.Domain.Mapping;
using BackupManager.Domain.Settings;
using BackupManager.Infra;
using BackupManager.Infra.Backup.Services;
using Microsoft.Extensions.Logging.Abstractions;
using JsonSerialization;
using Temporaries;
using Xunit;

namespace BackupManagerTests.Infra.Backup.Services;

public class DriveBackupServiceTests : TestsBase
{
    [Fact]
    public void BackupFiles_FilesAndDirectoriesToBackup_DestRelativeDirectoryConfigured_FilesAndDirectoriesAreBackupedIntoDestRelativeDirectory()
    {
        JsonSerializer objectSerializer = new();

        BackupSettings backupSettings = new()
        {
            RootDirectory = Directory.GetCurrentDirectory(),
            ShouldBackupToKnownDirectory = true,
            DirectoriesSourcesToDirectoriesDestinationMap = new List<DirectoriesMap>
            {
                new()
                {
                    SourceRelativeDirectory = "Games",
                    DestRelativeDirectory = "GamesBackup"
                },
                new()
                {
                    SourceRelativeDirectory = "Documents and important files",
                    DestRelativeDirectory = "Files/Documents and important files",
                }
            }
        };
        
        FilesHashesHandler filesHashesHandler = new(objectSerializer, NullLogger<FilesHashesHandler>.Instance);

        using TempDirectory gamesTempDirectory = CreateFilesToBackup();
        
        DriveBackupService backupService = new(filesHashesHandler, NullLoggerFactory.Instance);

        backupService.BackupFiles(backupSettings, CancellationToken.None);

        Assert.Equal("just a file", File.ReadAllText(Path.Combine(Consts.BackupsDirectoryPath, "GamesBackup" ,"file in games directory.txt")));
        Assert.Equal("save the princess!", File.ReadAllText(Path.Combine(Consts.BackupsDirectoryPath, "GamesBackup", "prince of persia" ,"file in prince of persia directory.txt")));

        string lastBackupTimeStr = File.ReadAllLines(Consts.BackupTimeDiaryFilePath)[^1];
        DateTime lastBackupTime = DateTime.Parse(lastBackupTimeStr);
        Assert.Equal(DateTime.Now.Date, lastBackupTime.Date);

        Dictionary<string, List<string>> hashesToFilePath = objectSerializer.Deserialize<Dictionary<string, List<string>>>(Consts.DataFilePath);
        Assert.Equal(Path.Combine("\\Games", "file in games directory.txt"), hashesToFilePath["5B0CCEF73B8DCF768B3EBCFBB902269389C0224202F120C1AA25137AC2C27551"][0]);
        Assert.Equal(Path.Combine("\\Games", "prince of persia", "file in prince of persia directory.txt"),
            hashesToFilePath["674833D4A3B3A2E67001316DE33E5024963B0C429AF2455FF55083BE16592D55"][0]);
        
        // Make sure source files were copied and not moved or deleted.
        Assert.True(File.Exists(Path.Combine(gamesTempDirectory.Path, "file in games directory.txt")));
        Assert.True(File.Exists(Path.Combine(gamesTempDirectory.Path, "prince of persia", "file in prince of persia directory.txt")));
    }
    
    [Fact]
    public void BackupFiles_FilesAndDirectoriesToBackup_EmptyDestRelativeDirectory_FilesAndDirectoriesAreBackupedIntoBackupDirectoryWithPreservingStructure()
    {
        JsonSerializer objectSerializer = new();

        BackupSettings backupSettings = new()
        {
            RootDirectory = Directory.GetCurrentDirectory(),
            ShouldBackupToKnownDirectory = true,
            DirectoriesSourcesToDirectoriesDestinationMap = new List<DirectoriesMap>
            {
                new()
                {
                    SourceRelativeDirectory = "Games",
                    DestRelativeDirectory = ""
                }
            }
        };
        
        FilesHashesHandler filesHashesHandler = new(objectSerializer, NullLogger<FilesHashesHandler>.Instance);

        using TempDirectory gamesTempDirectory = CreateFilesToBackup();
        
        DriveBackupService backupService = new(filesHashesHandler, NullLoggerFactory.Instance);

        backupService.BackupFiles(backupSettings, CancellationToken.None);

        Assert.Equal("just a file", File.ReadAllText(Path.Combine(Consts.BackupsDirectoryPath, "Games" ,"file in games directory.txt")));
        Assert.Equal("save the princess!", File.ReadAllText(Path.Combine(Consts.BackupsDirectoryPath, "Games", "prince of persia" ,"file in prince of persia directory.txt")));

        string lastBackupTimeStr = File.ReadAllLines(Consts.BackupTimeDiaryFilePath)[^1];
        DateTime lastBackupTime = DateTime.Parse(lastBackupTimeStr);
        Assert.Equal(DateTime.Now.Date, lastBackupTime.Date);

        Dictionary<string, List<string>> hashesToFilePath = objectSerializer.Deserialize<Dictionary<string, List<string>>>(Consts.DataFilePath);
        Assert.Equal(Path.Combine("\\Games", "file in games directory.txt"), hashesToFilePath["5B0CCEF73B8DCF768B3EBCFBB902269389C0224202F120C1AA25137AC2C27551"][0]);
        Assert.Equal(Path.Combine("\\Games", "prince of persia", "file in prince of persia directory.txt"),
            hashesToFilePath["674833D4A3B3A2E67001316DE33E5024963B0C429AF2455FF55083BE16592D55"][0]);
        
        // Make sure source files were copied and not moved or deleted.
        Assert.True(File.Exists(Path.Combine(gamesTempDirectory.Path, "file in games directory.txt")));
        Assert.True(File.Exists(Path.Combine(gamesTempDirectory.Path, "prince of persia", "file in prince of persia directory.txt")));
    }

    private TempDirectory CreateFilesToBackup()
    {
        TempDirectory gamesDirectory = new("Games");
        TempDirectory princeOfPersiaDirectory = new(Path.Combine(gamesDirectory.Path, "prince of persia"));
        
        File.WriteAllText(Path.Combine(gamesDirectory.Path, "file in games directory.txt"), "just a file");
        File.WriteAllText(Path.Combine(princeOfPersiaDirectory.Path, "file in prince of persia directory.txt"), "save the princess!");

        return gamesDirectory;
    }
}