using BackupManager.Domain.Configuration;
using BackupManager.Infra.Backup.Detectors;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using JsonSerialization;
using Xunit;

namespace BackupManagerTests.Infra.Backup.Detectors;

public class BackupOptionsDetectorTests
{
    [Fact(Skip = "Requires real device connected")]
    // [Fact]
    public void DetectBackupOptions_RealDeviceConnected_DeviceHasNoConfigurationFiles_ReturnNull()
    {
        BackupOptionsDetector backupOptionsDetector = new(A.Dummy<IOptions<BackupServiceConfiguration>>(),
            new JsonSerializer(),
            NullLogger<BackupOptionsDetector>.Instance);
        
        Assert.Null(backupOptionsDetector.DetectBackupOptions());
    }
}