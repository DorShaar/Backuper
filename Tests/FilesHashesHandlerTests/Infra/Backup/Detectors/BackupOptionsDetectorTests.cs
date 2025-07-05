using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.Domain.Configuration;
using BackupManager.Domain.Settings;
using BackupManager.Infra.Backup.Detectors;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BackupManagerTests.Infra.Backup.Detectors;

public class BackupOptionsDetectorTests : TestsBase
{
    [Fact(Skip = "Requires real device connected without BackupSettings.json")]
    // [Fact]
    public async Task DetectBackupOptions_RealDeviceConnected_DeviceHasNoSettingsFile_ReturnNull()
    {
        BackupSettingsDetector backupSettingsDetector = new(A.Dummy<IOptions<BackupServiceConfiguration>>(),
            mJsonSerializer,
            NullLogger<BackupSettingsDetector>.Instance);
        
        Assert.Null(await backupSettingsDetector.DetectBackupSettings(CancellationToken.None));
    }
    
    [Fact(Skip = "Requires real device connected with BackupSettings.json without RootDirectory Section")]
    // [Fact]
    public async Task DetectBackupOptions_RealDeviceConnected_DeviceHasSettingsFileWithoutRootDirectorySection_ReturnSettingsWithRootDirectory()
    {
        BackupSettingsDetector backupSettingsDetector = new(A.Dummy<IOptions<BackupServiceConfiguration>>(),
            mJsonSerializer,
            NullLogger<BackupSettingsDetector>.Instance);

        List<BackupSettings>? settingsList = await backupSettingsDetector.DetectBackupSettings(CancellationToken.None);
        Assert.NotNull(settingsList);
        BackupSettings settings = settingsList[0];
        Assert.Equal("Internal shared storage", settings.RootDirectory); 
    }
}