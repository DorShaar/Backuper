using System;
using System.IO;
using BackupManagerCore;
using JsonSerialization;
using OSOperations;
using Xunit;

namespace BackupManagerTests;

public class TestsBase : IDisposable
{
    protected readonly IJsonSerializer mJsonSerializer = new JsonSerializer();
    
    protected TestsBase()
    {
        if (!Admin.IsRunningAsAdministrator())
        {
            Assert.Fail("Must run tests with admin privileges");
        }
        
        if (Path.GetFileName(Path.GetDirectoryName(Consts.Data.DataDirectoryPath)) == Consts.BackupServiceDirectoryName)
        {
            Assert.Fail("Cannot run tests on real directory. Please Change it before running tests.");
        }

        _ = Directory.CreateDirectory(Consts.Data.DataDirectoryPath);
    }

    public void Dispose()
    {
        string backupServiceDirectoryPath = Path.GetDirectoryName(Consts.Data.DataDirectoryPath)!;
        if (Directory.Exists(backupServiceDirectoryPath))
        {
            Directory.Delete(backupServiceDirectoryPath, recursive: true);
        }
    }

    protected string GetBackedUpFilesLocalFilePath(string databaseName = Consts.Database.BackupFilesCollectionName)
    {
        return Path.Combine(Consts.Data.DataDirectoryPath, $"{databaseName}.{Consts.Data.LocalDatabaseExtension}");
    }
}