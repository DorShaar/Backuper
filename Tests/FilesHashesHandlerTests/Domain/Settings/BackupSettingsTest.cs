using System.Collections.Generic;
using BackupManager.Domain.Mapping;
using BackupManager.Domain.Settings;
using JsonSerialization;
using Temporaries;
using Xunit;

namespace BackupManagerTests.Domain.Settings;

public class BackupSettingsTest
{
    [Fact]
    public void Deserialize_ValidInput_PerformedAsExpected()
    {
        BackupSettings settings = new()
        {
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
            },
            RootDirectory = "D",
            ShouldBackupToKnownDirectory = false
        };

        JsonSerializer jsonSerializer = new();

        using TempFile settingsFile = new();
        
        jsonSerializer.Serialize(settings, settingsFile.Path);
        BackupSettings deserializedSettings = jsonSerializer.Deserialize<BackupSettings>(settingsFile.Path);
        
        Assert.Equal("Games", deserializedSettings.DirectoriesSourcesToDirectoriesDestinationMap[0].SourceRelativeDirectory);
        Assert.Equal("GamesBackup", deserializedSettings.DirectoriesSourcesToDirectoriesDestinationMap[0].DestRelativeDirectory);
        Assert.Equal("Documents and important files", deserializedSettings.DirectoriesSourcesToDirectoriesDestinationMap[1].SourceRelativeDirectory);
        Assert.Equal("Files/Documents and important files", deserializedSettings.DirectoriesSourcesToDirectoriesDestinationMap[1].DestRelativeDirectory);
        
        Assert.Equal(settings.RootDirectory, deserializedSettings.RootDirectory);
        Assert.Equal(settings.ShouldBackupToKnownDirectory, deserializedSettings.ShouldBackupToKnownDirectory);
    }
}