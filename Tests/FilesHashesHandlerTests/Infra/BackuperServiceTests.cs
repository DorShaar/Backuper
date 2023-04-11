using System.Collections.Generic;
using System.IO;
using System.Threading;
using Backuper.App.Serialization;
using Backuper.Domain.Configuration;
using Backuper.Domain.Mapping;
using Backuper.Infra;
using BackupManager.Infra;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Temporaries;
using Xunit;

namespace BackupManagerTests.Infra;

public class BackuperServiceTests
{
    [Fact]
    public void BackupFiles_FilesAndDirectoriesToBackup_FilesAndDirectoriesAreBackuped()
    {
        IObjectSerializer objectSerializer = A.Dummy<IObjectSerializer>();

        BackuperConfiguration configuration = new()
        {
            RootDirectory = Directory.GetCurrentDirectory(),
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
        
        IOptions<BackuperConfiguration> options = Options.Create(configuration);
        
        FilesHashesHandler filesHashesHandler = new(objectSerializer, options, NullLogger<FilesHashesHandler>.Instance);

        using TempDirectory gamesTempDirectory = CreateFilesToBackup();
        
        BackuperService backuperService = new(filesHashesHandler, options, NullLogger<BackuperService>.Instance);

        backuperService.BackupFiles(CancellationToken.None);

        Assert.Equal("just a file", File.ReadAllText(Path.Combine(Consts.DataDirectoryPath, "GamesBackup" ,"file in games directory.txt")));
        Assert.Equal("save the princess!", File.ReadAllText(Path.Combine(Consts.DataDirectoryPath, "GamesBackup", "prince of persia" ,"file in prince of persia directory.txt")));
        
        // TODO DOR make sure tests are safe and will not harm the real time backup.
        
        
        // TODO DOR add in test  backTimeDiary was updated
        
        // TODO DOR add in test hashes were updated.
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