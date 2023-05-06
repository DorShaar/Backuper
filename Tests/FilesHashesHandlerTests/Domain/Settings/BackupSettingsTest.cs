using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.Domain.Mapping;
using BackupManager.Domain.Settings;
using Temporaries;
using Xunit;

namespace BackupManagerTests.Domain.Settings;

public class BackupSettingsTest : TestsBase
{
    [Fact]
    public async Task Deserialize_ValidInput_PerformedAsExpected()
    {
        BackupSerializedSettings settings = new()
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
            ShouldBackupToKnownDirectory = false
        };

        using TempFile settingsFile = new();
        
        await mJsonSerializer.SerializeAsync(settings, settingsFile.Path, CancellationToken.None).ConfigureAwait(false);
        BackupSerializedSettings deserializedSettings =
            await mJsonSerializer.DeserializeAsync<BackupSerializedSettings>(settingsFile.Path, CancellationToken.None).ConfigureAwait(false);
        
        Assert.Equal("Games", deserializedSettings.DirectoriesSourcesToDirectoriesDestinationMap[0].SourceRelativeDirectory);
        Assert.Equal("GamesBackup", deserializedSettings.DirectoriesSourcesToDirectoriesDestinationMap[0].DestRelativeDirectory);
        Assert.Equal("Documents and important files", deserializedSettings.DirectoriesSourcesToDirectoriesDestinationMap[1].SourceRelativeDirectory);
        Assert.Equal("Files/Documents and important files", deserializedSettings.DirectoriesSourcesToDirectoriesDestinationMap[1].DestRelativeDirectory);
        
        Assert.Equal(settings.ShouldBackupToKnownDirectory, deserializedSettings.ShouldBackupToKnownDirectory);
    }
}