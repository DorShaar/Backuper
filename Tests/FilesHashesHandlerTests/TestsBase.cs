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
    }

    public void Dispose()
    {
        Directory.Delete(Path.GetDirectoryName(Consts.DataDirectoryPath), recursive: true);
    }
}