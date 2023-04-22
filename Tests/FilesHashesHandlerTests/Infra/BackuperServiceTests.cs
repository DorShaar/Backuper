using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BackupManager.Domain.Mapping;
using BackupManager.Domain.Settings;
using BackupManager.Infra.Serialization;
using BackupManager.Infra;
using Microsoft.Extensions.Logging.Abstractions;
using Temporaries;
using Xunit;

namespace BackupManagerTests.Infra;

public class BackuperServiceTests : TestsBase
{
    [Fact]
    public void BackupFiles_FilesAndDirectoriesToBackup_FilesAndDirectoriesAreBackuped()
    {
        JsonSerializerWrapper objectSerializer = new();

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
        
        BackupService backupService = new(filesHashesHandler, NullLogger<BackupService>.Instance);

        backupService.BackupFiles(backupSettings, CancellationToken.None);

        Assert.Equal("just a file", File.ReadAllText(Path.Combine(Consts.DataDirectoryPath, "GamesBackup" ,"file in games directory.txt")));
        Assert.Equal("save the princess!", File.ReadAllText(Path.Combine(Consts.DataDirectoryPath, "GamesBackup", "prince of persia" ,"file in prince of persia directory.txt")));

        string lastBackupTimeStr = File.ReadAllLines(Consts.BackupTimeDiaryFilePath)[^1];
        DateTime lastBackupTime = DateTime.Parse(lastBackupTimeStr);
        Assert.Equal(DateTime.Now.Date, lastBackupTime.Date);

        List<string> hashes = objectSerializer.Deserialize<Dictionary<string, List<string>>>(Consts.DataFilePath).Keys.ToList();
        Assert.Equal("5B0CCEF73B8DCF768B3EBCFBB902269389C0224202F120C1AA25137AC2C27551", hashes[0]);
        Assert.Equal("674833D4A3B3A2E67001316DE33E5024963B0C429AF2455FF55083BE16592D55", hashes[1]);
    }

    // TODO DOR tests backup not performed when already backuped today, use this test in it another place.
    // [Fact]
    // public void BackupFiles_FilesAndDirectoriesToBackupExists_AlreadyPerformedBackupToday_DoesNotBackup()
    // {
    //     IObjectSerializer objectSerializer = A.Dummy<IObjectSerializer>();
    //
    //     BackuperConfiguration configuration = new()
    //     {
    //         RootDirectory = Directory.GetCurrentDirectory(),
    //         DirectoriesSourcesToDirectoriesDestinationMap = new List<DirectoriesMap>
    //         {
    //             new()
    //             {
    //                 SourceRelativeDirectory = "Games",
    //                 DestRelativeDirectory = "GamesBackup"
    //             },
    //             new()
    //             {
    //                 SourceRelativeDirectory = "Documents and important files",
    //                 DestRelativeDirectory = "Files/Documents and important files",
    //             }
    //         }
    //     };
    //     
    //     IOptions<BackuperConfiguration> options = Options.Create(configuration);
    //     
    //     FilesHashesHandler filesHashesHandler = new(objectSerializer, options, NullLogger<FilesHashesHandler>.Instance);
    //
    //     using TempDirectory gamesTempDirectory = CreateFilesToBackup();
    //     
    //     BackuperService backuperService = new(filesHashesHandler, options, NullLogger<BackuperService>.Instance);
    //
    //     // Making sure backup time diary contains todays backup
    //     string? backuperDirectoryName = Path.GetDirectoryName(Consts.BackupTimeDiaryFilePath);
    //     Assert.NotNull(backuperDirectoryName);
    //     _ = Directory.CreateDirectory(backuperDirectoryName);
    //     File.AppendAllText(Consts.BackupTimeDiaryFilePath, DateTime.Now + Environment.NewLine);
    //     
    //     backuperService.BackupFiles(CancellationToken.None);
    //
    //     Assert.Equal(1, File.ReadAllLines(Consts.BackupTimeDiaryFilePath).Length);
    //     
    //     Assert.Empty(Directory.EnumerateFiles(Consts.DataDirectoryPath, "*", SearchOption.AllDirectories));
    // }

    private TempDirectory CreateFilesToBackup()
    {
        TempDirectory gamesDirectory = new("Games");
        TempDirectory princeOfPersiaDirectory = new(Path.Combine(gamesDirectory.Path, "prince of persia"));
        
        File.WriteAllText(Path.Combine(gamesDirectory.Path, "file in games directory.txt"), "just a file");
        File.WriteAllText(Path.Combine(princeOfPersiaDirectory.Path, "file in prince of persia directory.txt"), "save the princess!");

        return gamesDirectory;
    }
}