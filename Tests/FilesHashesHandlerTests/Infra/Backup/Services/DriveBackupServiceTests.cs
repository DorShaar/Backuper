using System;
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
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Temporaries;
using Xunit;

namespace BackupManagerTests.Infra.Backup.Services;

[Collection("BackupServiceTests Usage")]
public class DriveBackupServiceTests : TestsBase
{
    [Fact]
    public async Task BackupFiles_FilesAndDirectoriesToBackup_DestRelativeDirectoryConfigured_FilesAndDirectoriesAreBackedUpIntoDestRelativeDirectory()
    {
        BackupSerializedSettings backupSerializedSettings = new()
        {
            IsFromInstallation = false,
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

        BackupSettings backupSettings = new(backupSerializedSettings, Directory.GetCurrentDirectory());
        
        LocalJsonDatabase localJsonDatabase = new(mJsonSerializer, NullLogger<LocalJsonDatabase>.Instance);
        FilesHashesHandler filesHashesHandler = new(new List<IBackedUpFilesDatabase> {localJsonDatabase});

        using TempDirectory gamesTempDirectory = CreateFilesToBackup();
        
        DriveBackupService backupService = new(filesHashesHandler, NullLoggerFactory.Instance);

        await backupService.BackupFiles(backupSettings, CancellationToken.None);

        Assert.Equal("just a file",
            await File.ReadAllTextAsync(Path.Combine(Consts.Data.WaitingApprovalDirectoryPath, "GamesBackup" ,"file in games directory.txt")));
        Assert.Equal("save the princess!",
            await File.ReadAllTextAsync(Path.Combine(Consts.Data.WaitingApprovalDirectoryPath, "GamesBackup", "prince of persia" ,"file in prince of persia directory.txt")));
        
        DateTime lastBackupTime = await ExtractLastBackupTime();
        Assert.Equal(DateTime.Now.Date, lastBackupTime.Date);

        Dictionary<string, List<string>> hashesToFilePath =
            await mJsonSerializer.DeserializeAsync<Dictionary<string, List<string>>>(GetBackedUpFilesLocalFilePath(), CancellationToken.None);
        Assert.Equal(Path.Combine("/Games/file in games directory.txt"), hashesToFilePath["5B0CCEF73B8DCF768B3EBCFBB902269389C0224202F120C1AA25137AC2C27551"][0]);
        Assert.Equal(Path.Combine("/Games/prince of persia/file in prince of persia directory.txt"),
            hashesToFilePath["674833D4A3B3A2E67001316DE33E5024963B0C429AF2455FF55083BE16592D55"][0]);
        
        // Make sure source files were copied and not moved or deleted.
        Assert.True(File.Exists(Path.Combine(gamesTempDirectory.Path, "file in games directory.txt")));
        Assert.True(File.Exists(Path.Combine(gamesTempDirectory.Path, "prince of persia", "file in prince of persia directory.txt")));
    }
    
    [Fact]
    public async Task BackupFiles_FilesAndDirectoriesToBackup_EmptyDestRelativeDirectory_FilesAndDirectoriesAreBackedUpIntoBackupDirectoryWithPreservingStructure()
    {
        BackupSerializedSettings backupSerializedSettings = new()
        {
            IsFromInstallation = false,
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
        
        BackupSettings backupSettings = new(backupSerializedSettings, Directory.GetCurrentDirectory());
        
        LocalJsonDatabase localJsonDatabase = new(mJsonSerializer, NullLogger<LocalJsonDatabase>.Instance);
        FilesHashesHandler filesHashesHandler = new(new List<IBackedUpFilesDatabase> {localJsonDatabase});

        using TempDirectory gamesTempDirectory = CreateFilesToBackup();
        
        DriveBackupService backupService = new(filesHashesHandler, NullLoggerFactory.Instance);

        await backupService.BackupFiles(backupSettings, CancellationToken.None);

        Assert.Equal("just a file",
            await File.ReadAllTextAsync(Path.Combine(Consts.Data.WaitingApprovalDirectoryPath, "Games" ,"file in games directory.txt")));
        Assert.Equal("save the princess!",
            await File.ReadAllTextAsync(Path.Combine(Consts.Data.WaitingApprovalDirectoryPath, "Games", "prince of persia" ,"file in prince of persia directory.txt")));

        DateTime lastBackupTime = await ExtractLastBackupTime();
        Assert.Equal(DateTime.Now.Date, lastBackupTime.Date);

        Dictionary<string, List<string>> hashesToFilePath =
            await mJsonSerializer.DeserializeAsync<Dictionary<string, List<string>>>(GetBackedUpFilesLocalFilePath(), CancellationToken.None);
        Assert.Equal(Path.Combine("/Games/file in games directory.txt"), hashesToFilePath["5B0CCEF73B8DCF768B3EBCFBB902269389C0224202F120C1AA25137AC2C27551"][0]);
        Assert.Equal(Path.Combine("/Games/prince of persia/file in prince of persia directory.txt"),
            hashesToFilePath["674833D4A3B3A2E67001316DE33E5024963B0C429AF2455FF55083BE16592D55"][0]);
        
        // Make sure source files were copied and not moved or deleted.
        Assert.True(File.Exists(Path.Combine(gamesTempDirectory.Path, "file in games directory.txt")));
        Assert.True(File.Exists(Path.Combine(gamesTempDirectory.Path, "prince of persia", "file in prince of persia directory.txt")));
    }
    
    [Fact]
    public async Task BackupFiles_ShouldBackupToKnownDirectoryIsFalse_RootDirectoryIsEmpty_NoBackupAndSavedPerformed()
    {
        BackupSerializedSettings backupSerializedSettings = new()
        {
            IsFromInstallation = true,
            ShouldBackupToKnownDirectory = false,
            Token = "some-token",
            DirectoriesSourcesToDirectoriesDestinationMap = new List<DirectoriesMap>
            {
                new()
                {
                    SourceRelativeDirectory = "Games",
                    DestRelativeDirectory = ""
                }
            }
        };

        BackupSettings backupSettings = new(backupSerializedSettings, string.Empty);

        IFilesHashesHandler filesHashesHandler = A.Fake<IFilesHashesHandler>();
        
        using TempDirectory gamesTempDirectory = CreateFilesToBackup(Consts.Data.ReadyToBackupDirectoryPath);
        
        DriveBackupService backupService = new(filesHashesHandler, NullLoggerFactory.Instance);

        await backupService.BackupFiles(backupSettings, CancellationToken.None);
        
        A.CallTo(() => filesHashesHandler.Save(A<CancellationToken>.Ignored)).MustNotHaveHappened();
    }
    
    [Fact]
    public async Task BackupFiles_FilesAndDirectoriesToBackup_ShouldBackupToKnownDirectoryIsFalse_FilesAndDirectoriesAreBackedUpIntoDestRelativeDirectory()
    {
        BackupSerializedSettings backupSerializedSettings = new()
        {
            IsFromInstallation = true,
            ShouldBackupToKnownDirectory = false,
            Token = "some_token",
            DirectoriesSourcesToDirectoriesDestinationMap = new List<DirectoriesMap>
            {
                new()
                {
                    SourceRelativeDirectory = "Games",
                    DestRelativeDirectory = "GamesBackup"
                }
            }
        };
        
        using TempDirectory tempBackupDirectory = new();

        BackupSettings backupSettings = new(backupSerializedSettings, tempBackupDirectory.Path);
        
        LocalJsonDatabase localJsonDatabase = new(mJsonSerializer, NullLogger<LocalJsonDatabase>.Instance);
        FilesHashesHandler filesHashesHandler = new(new List<IBackedUpFilesDatabase> {localJsonDatabase});
    
        using TempDirectory gamesTempDirectory = CreateFilesToBackup(Consts.Data.ReadyToBackupDirectoryPath);
        
        DriveBackupService backupService = new(filesHashesHandler, NullLoggerFactory.Instance);
    
        await backupService.BackupFiles(backupSettings, CancellationToken.None);
    
        Assert.Equal("just a file",
            await File.ReadAllTextAsync(Path.Combine(tempBackupDirectory.Path, "GamesBackup" ,"file in games directory.txt")));
        Assert.Equal("save the princess!",
            await File.ReadAllTextAsync(Path.Combine(tempBackupDirectory.Path, "GamesBackup", "prince of persia" ,"file in prince of persia directory.txt")));
    
        DateTime lastBackupTime = await ExtractLastBackupTime();
        Assert.Equal(DateTime.Now.Date, lastBackupTime.Date);

        string databaseName = string.Format(Consts.Database.BackupFilesForKnownDriveCollectionTemplate, backupSerializedSettings.Token); 
        Dictionary<string, List<string>> hashesToFilePath =
            await mJsonSerializer.DeserializeAsync<Dictionary<string, List<string>>>(GetBackedUpFilesLocalFilePath(databaseName), CancellationToken.None);
        Assert.Equal(Path.Combine("/Games/file in games directory.txt"), hashesToFilePath["5B0CCEF73B8DCF768B3EBCFBB902269389C0224202F120C1AA25137AC2C27551"][0]);
        Assert.Equal(Path.Combine("/Games/prince of persia/file in prince of persia directory.txt"),
            hashesToFilePath["674833D4A3B3A2E67001316DE33E5024963B0C429AF2455FF55083BE16592D55"][0]);
        
        // Makes sure files in ReadyToBackup directory were moved to Backedup directory.
        Assert.False(File.Exists(Path.Combine(gamesTempDirectory.Path, "file in games directory.txt")));
        Assert.True(File.Exists(Path.Combine(Consts.Data.BackedUpDirectoryPath, "Games", "file in games directory.txt")));
        Assert.False(File.Exists(Path.Combine(gamesTempDirectory.Path, "prince of persia", "file in prince of persia directory.txt")));
        Assert.True(File.Exists(Path.Combine(Consts.Data.BackedUpDirectoryPath, "Games", "prince of persia", "file in prince of persia directory.txt")));
    }

    [Fact]
    public async Task BackupFiles_NotAllFilesInDestinationAreMapped_ShouldBackupToKnownDirectoryIsFalse_MapNewFilesWithHashes()
    {
        BackupSerializedSettings backupSerializedSettings = new()
        {
            IsFromInstallation = true,
            ShouldBackupToKnownDirectory = false,
            Token = "some_token",
            DirectoriesSourcesToDirectoriesDestinationMap = new List<DirectoriesMap>
            {
                // Should be empty to make sure no real backup is performed but we do map the destination directory first.
            }
        };
    
        using TempDirectory tempBackupDirectory = new();
        BackupSettings backupSettings = new(backupSerializedSettings, tempBackupDirectory.Path);
        
        LocalJsonDatabase localJsonDatabase = new(mJsonSerializer, NullLogger<LocalJsonDatabase>.Instance);
        FilesHashesHandler filesHashesHandler = new(new List<IBackedUpFilesDatabase> {localJsonDatabase});
        await filesHashesHandler.LoadDatabase("Data-some_token", CancellationToken.None);
    
        DriveBackupService backupService = new(filesHashesHandler, NullLoggerFactory.Instance);
    
        using TempDirectory gamesTempDirectory = CreateFilesToBackup(Consts.Data.ReadyToBackupDirectoryPath);
        string alreadyExistingFilePath = Path.Combine(tempBackupDirectory.Path, "Games", "file in games directory.txt");
        _ = Directory.CreateDirectory(Path.GetDirectoryName(alreadyExistingFilePath) ?? throw new NullReferenceException());
        await File.WriteAllTextAsync(alreadyExistingFilePath, "just a file");
        
        Assert.False(await filesHashesHandler.IsHashExists("5B0CCEF73B8DCF768B3EBCFBB902269389C0224202F120C1AA25137AC2C27551", CancellationToken.None));
        
        await backupService.BackupFiles(backupSettings, CancellationToken.None);
        
        Assert.True(await filesHashesHandler.IsHashExists("5B0CCEF73B8DCF768B3EBCFBB902269389C0224202F120C1AA25137AC2C27551", CancellationToken.None));
    }

    private static TempDirectory CreateFilesToBackup(string? directory = null)
    {
        string gamesDirectoryPath = Path.Combine(directory ?? string.Empty, "Games");
        TempDirectory gamesDirectory = new(gamesDirectoryPath);
        TempDirectory princeOfPersiaDirectory = new(Path.Combine(gamesDirectory.Path, "prince of persia"));
        
        File.WriteAllText(Path.Combine(gamesDirectory.Path, "file in games directory.txt"), "just a file");
        File.WriteAllText(Path.Combine(princeOfPersiaDirectory.Path, "file in prince of persia directory.txt"), "save the princess!");

        return gamesDirectory;
    }

    private static async Task<DateTime> ExtractLastBackupTime()
    {
        string lastBackupLog = (await File.ReadAllLinesAsync(Consts.Data.BackupTimeDiaryFilePath).ConfigureAwait(false))[^1];
        int lastIndexOfTime = lastBackupLog.IndexOf("---", StringComparison.Ordinal);
        string lastBackupTimeText = lastBackupLog[..lastIndexOfTime];
        return DateTime.Parse(lastBackupTimeText);
    }
}