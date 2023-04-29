using System;
using System.IO;
using BackupManager.Infra;
using OSOperations;
using Xunit;

namespace BackupManagerTests;

public class TestsBase : IDisposable
{
    protected TestsBase()
    {
        if (!Admin.IsRunningAsAdministrator())
        {
            Assert.Fail("Must run tests with admin privileges");
        }
        
        if (Path.GetFileName(Path.GetDirectoryName(Consts.DataDirectoryPath)) == Consts.BackupServiceDirectoryName)
        {
            Assert.Fail("Cannot run tests on real directory. Please Change it before running tests.");
        }

        _ = Directory.CreateDirectory(Consts.DataDirectoryPath);
    }

    public void Dispose()
    {
        string backupServiceDirectoryPath = Path.GetDirectoryName(Consts.DataDirectoryPath)!;
        if (Directory.Exists(backupServiceDirectoryPath))
        {
            Directory.Delete(backupServiceDirectoryPath, recursive: true);
        }
    }
}