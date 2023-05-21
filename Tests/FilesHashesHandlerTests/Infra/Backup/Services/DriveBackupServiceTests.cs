﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.App.Database;
using BackupManager.Domain.Mapping;
using BackupManager.Domain.Settings;
using BackupManager.Infra;
using BackupManager.Infra.Backup.Services;
using BackupManager.Infra.DB.LocalJsonFileDatabase;
using BackupManager.Infra.FileHashHandlers;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Temporaries;
using Xunit;

namespace BackupManagerTests.Infra.Backup.Services;

public class DriveBackupServiceTests : TestsBase
{
    [Fact]
    public async Task BackupFiles_FilesAndDirectoriesToBackup_DestRelativeDirectoryConfigured_FilesAndDirectoriesAreBackedUpIntoDestRelativeDirectory()
    {
        BackupSerializedSettings backupSerializedSettings = new()
        {
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
        
        BackupSettings backupSettings = new(backupSerializedSettings)
        {
            RootDirectory = Directory.GetCurrentDirectory()
        };
        
        LocalJsonDatabase localJsonDatabase = new(mJsonSerializer, NullLogger<LocalJsonDatabase>.Instance);
        FilesHashesHandler filesHashesHandler = new(localJsonDatabase, NullLogger<FilesHashesHandler>.Instance);

        using TempDirectory gamesTempDirectory = CreateFilesToBackup();
        
        DriveBackupService backupService = new(filesHashesHandler, NullLoggerFactory.Instance);

        await backupService.BackupFiles(backupSettings, CancellationToken.None).ConfigureAwait(false);

        Assert.Equal("just a file",
            await File.ReadAllTextAsync(Path.Combine(Consts.BackupsDirectoryPath, "GamesBackup" ,"file in games directory.txt")).ConfigureAwait(false));
        Assert.Equal("save the princess!",
            await File.ReadAllTextAsync(Path.Combine(Consts.BackupsDirectoryPath, "GamesBackup", "prince of persia" ,"file in prince of persia directory.txt")).ConfigureAwait(false));
        
        DateTime lastBackupTime = await ExtractLastBackupTime().ConfigureAwait(false);
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
        
        BackupSettings backupSettings = new(backupSerializedSettings)
        {
            RootDirectory = Directory.GetCurrentDirectory()
        };
        
        LocalJsonDatabase localJsonDatabase = new(mJsonSerializer, NullLogger<LocalJsonDatabase>.Instance);
        FilesHashesHandler filesHashesHandler = new(localJsonDatabase, NullLogger<FilesHashesHandler>.Instance);

        using TempDirectory gamesTempDirectory = CreateFilesToBackup();
        
        DriveBackupService backupService = new(filesHashesHandler, NullLoggerFactory.Instance);

        await backupService.BackupFiles(backupSettings, CancellationToken.None).ConfigureAwait(false);

        Assert.Equal("just a file",
            await File.ReadAllTextAsync(Path.Combine(Consts.BackupsDirectoryPath, "Games" ,"file in games directory.txt")).ConfigureAwait(false));
        Assert.Equal("save the princess!",
            await File.ReadAllTextAsync(Path.Combine(Consts.BackupsDirectoryPath, "Games", "prince of persia" ,"file in prince of persia directory.txt")).ConfigureAwait(false));

        DateTime lastBackupTime = await ExtractLastBackupTime().ConfigureAwait(false);
        Assert.Equal(DateTime.Now.Date, lastBackupTime.Date);

        Dictionary<string, List<string>> hashesToFilePath =
            await mJsonSerializer.DeserializeAsync<Dictionary<string, List<string>>>(GetBackedUpFilesLocalFilePath(), CancellationToken.None).ConfigureAwait(false);
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

        BackupSettings backupSettings = new(backupSerializedSettings);

        IFilesHashesHandler filesHashesHandler = A.Fake<IFilesHashesHandler>();
        
        using TempDirectory gamesTempDirectory = CreateFilesToBackup(Consts.BackupsDirectoryPath);
        
        DriveBackupService backupService = new(filesHashesHandler, NullLoggerFactory.Instance);

        await backupService.BackupFiles(backupSettings, CancellationToken.None).ConfigureAwait(false);
        
        A.CallTo(() => filesHashesHandler.Save(A<CancellationToken>.Ignored)).MustNotHaveHappened();
    }
    
    [Fact]
    public async Task BackupFiles_FilesAndDirectoriesToBackup_ShouldBackupToKnownDirectoryIsFalse_FilesAndDirectoriesAreBackedUpIntoDestRelativeDirectory()
    {
        BackupSerializedSettings backupSerializedSettings = new()
        {
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
    
        BackupSettings backupSettings = new(backupSerializedSettings)
        {
            RootDirectory = tempBackupDirectory.Path
        };
        
        LocalJsonDatabase localJsonDatabase = new(mJsonSerializer, NullLogger<LocalJsonDatabase>.Instance);
        FilesHashesHandler filesHashesHandler = new(localJsonDatabase, NullLogger<FilesHashesHandler>.Instance);
    
        using TempDirectory gamesTempDirectory = CreateFilesToBackup(Consts.BackupsDirectoryPath);
        
        DriveBackupService backupService = new(filesHashesHandler, NullLoggerFactory.Instance);
    
        await backupService.BackupFiles(backupSettings, CancellationToken.None).ConfigureAwait(false);
    
        Assert.Equal("just a file",
            await File.ReadAllTextAsync(Path.Combine(tempBackupDirectory.Path, "GamesBackup" ,"file in games directory.txt")).ConfigureAwait(false));
        Assert.Equal("save the princess!",
            await File.ReadAllTextAsync(Path.Combine(tempBackupDirectory.Path, "GamesBackup", "prince of persia" ,"file in prince of persia directory.txt")).ConfigureAwait(false));
    
        DateTime lastBackupTime = await ExtractLastBackupTime().ConfigureAwait(false);
        Assert.Equal(DateTime.Now.Date, lastBackupTime.Date);

        string databaseName = string.Format(Consts.BackupFilesForKnownDriveCollectionTemplate, backupSerializedSettings.Token); 
        Dictionary<string, List<string>> hashesToFilePath =
            await mJsonSerializer.DeserializeAsync<Dictionary<string, List<string>>>(GetBackedUpFilesLocalFilePath(databaseName), CancellationToken.None);
        Assert.Equal(Path.Combine("/Games/file in games directory.txt"), hashesToFilePath["5B0CCEF73B8DCF768B3EBCFBB902269389C0224202F120C1AA25137AC2C27551"][0]);
        Assert.Equal(Path.Combine("/Games/prince of persia/file in prince of persia directory.txt"),
            hashesToFilePath["674833D4A3B3A2E67001316DE33E5024963B0C429AF2455FF55083BE16592D55"][0]);
        
        // Make sure source files were copied and not moved or deleted.
        Assert.True(File.Exists(Path.Combine(gamesTempDirectory.Path, "file in games directory.txt")));
        Assert.True(File.Exists(Path.Combine(gamesTempDirectory.Path, "prince of persia", "file in prince of persia directory.txt")));
    }

    // TODO DOR now validate is working.
    [Fact]
    public async Task BackupFiles_NotAllFilesInDestinationAreMapped_ShouldBackupToKnownDirectoryIsFalse_MapNewFilesWithHashes()
    {
        BackupSerializedSettings backupSerializedSettings = new()
        {
            ShouldBackupToKnownDirectory = false,
            Token = "some_token",
            DirectoriesSourcesToDirectoriesDestinationMap = new List<DirectoriesMap>
            {
                // Should be empty to make sure no real backup is performed but we do map the destination directory first.
            }
        };

        using TempDirectory tempBackupDirectory = new();
        BackupSettings backupSettings = new(backupSerializedSettings)
        {
            RootDirectory = tempBackupDirectory.Path
        };
        
        LocalJsonDatabase localJsonDatabase = new(mJsonSerializer, NullLogger<LocalJsonDatabase>.Instance);
        FilesHashesHandler filesHashesHandler = new(localJsonDatabase, NullLogger<FilesHashesHandler>.Instance);

        DriveBackupService backupService = new(filesHashesHandler, NullLoggerFactory.Instance);

        using TempDirectory gamesTempDirectory = CreateFilesToBackup(Consts.BackupsDirectoryPath);
        string alreadyExistingFilePath = Path.Combine(tempBackupDirectory.Path, "Games", "file in games directory.txt"); 
        await File.WriteAllTextAsync(alreadyExistingFilePath, "just a file").ConfigureAwait(false);
        await filesHashesHandler.AddFileHash("5B0CCEF73B8DCF768B3EBCFBB902269389C0224202F120C1AA25137AC2C27551", alreadyExistingFilePath, CancellationToken.None).ConfigureAwait(false);

        Assert.False(await filesHashesHandler.IsHashExists("674833D4A3B3A2E67001316DE33E5024963B0C429AF2455FF55083BE16592D55", CancellationToken.None).ConfigureAwait(false));
        
        await backupService.BackupFiles(backupSettings, CancellationToken.None).ConfigureAwait(false);
        
        Assert.True(await filesHashesHandler.IsHashExists("674833D4A3B3A2E67001316DE33E5024963B0C429AF2455FF55083BE16592D55", CancellationToken.None).ConfigureAwait(false));
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
        string lastBackupLog = (await File.ReadAllLinesAsync(Consts.BackupTimeDiaryFilePath).ConfigureAwait(false))[^1];
        int lastIndexOfTime = lastBackupLog.IndexOf("---", StringComparison.Ordinal);
        string lastBackupTimeText = lastBackupLog[..lastIndexOfTime];
        return DateTime.Parse(lastBackupTimeText);
    }
}