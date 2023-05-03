using System.Collections.Generic;
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
    public void DetectBackupOptions_RealDeviceConnected_DeviceHasNoSettingsFile_ReturnNull()
    {
        BackupOptionsDetector backupOptionsDetector = new(A.Dummy<IOptions<BackupServiceConfiguration>>(),
            mJsonSerializer,
            NullLogger<BackupOptionsDetector>.Instance);
        
        Assert.Null(backupOptionsDetector.DetectBackupOptions());
    }
    
    [Fact(Skip = "Requires real device connected with BackupSettings.json without RootDirectory Section")]
    // [Fact]
    public void DetectBackupOptions_RealDeviceConnected_DeviceHasSettingsFileWithoutRootDirectorySection_ReturnSettingsWithRootDirectory()
    {
        BackupOptionsDetector backupOptionsDetector = new(A.Dummy<IOptions<BackupServiceConfiguration>>(),
            mJsonSerializer,
            NullLogger<BackupOptionsDetector>.Instance);

        List<BackupSettings>? settingsList = backupOptionsDetector.DetectBackupOptions();
        Assert.NotNull(settingsList);
        BackupSettings settings = settingsList[0];
        Assert.Equal("Internal shared storage", settings.RootDirectory); 
    }
}