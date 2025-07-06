using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.App.Database;
using BackupManager.Infra.DB.LocalJsonFileDatabase;
using BackupManager.Infra.FileHashHandlers;
using BackupManagerCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BackupManagerTests.Domain.Hash
{
    [Collection("BackupServiceTests Usage")]
    public class FilesHashesHandlerTests : TestsBase
    {
        [Fact]
        public async Task AddFileHash_FileHashNotExists_FileHashAdded()
        {
            LocalJsonDatabase localJsonDatabase = new(mJsonSerializer, NullLogger<LocalJsonDatabase>.Instance);
            await localJsonDatabase.Load(Consts.Database.BackupFilesCollectionName, CancellationToken.None);
            FilesHashesHandler filesHashesHandler = new(new List<IBackedUpFilesDatabase> {localJsonDatabase});

            await filesHashesHandler.AddFileHash("ABC123", "FileName.ext", CancellationToken.None);
            await filesHashesHandler.AddFileHash("ABC1234", "FileName.ext", CancellationToken.None);
            
            Assert.True(await filesHashesHandler.IsHashExists("ABC123", CancellationToken.None));
            Assert.True(await filesHashesHandler.IsHashExists("ABC1234", CancellationToken.None));
            Assert.True(await filesHashesHandler.IsFilePathExist("FileName.ext", CancellationToken.None));
        }

        [Fact]
        public async Task AddFileHash_FileHashAlreadyExists_PathIsAddedToTheSameHash()
        {
            LocalJsonDatabase localJsonDatabase = new(mJsonSerializer, NullLogger<LocalJsonDatabase>.Instance);
            await localJsonDatabase.Load(Consts.Database.BackupFilesCollectionName, CancellationToken.None);
            FilesHashesHandler filesHashesHandler = new(new List<IBackedUpFilesDatabase> {localJsonDatabase});
            
            const string firstFileName = "FileName.ext";
            const string secondFileName = "FileName2.ext";
            const string hash = "ABC123";
            await filesHashesHandler.AddFileHash(hash, firstFileName, CancellationToken.None);
            await filesHashesHandler.AddFileHash(hash, secondFileName, CancellationToken.None);
            
            Assert.True(await filesHashesHandler.IsFilePathExist(firstFileName, CancellationToken.None));
            Assert.True(await filesHashesHandler.IsFilePathExist(secondFileName, CancellationToken.None));
            Assert.True(await filesHashesHandler.IsHashExists(hash, CancellationToken.None));
        }
        
        [Fact]
        public async Task AddFileHash_FileHashAlreadyExists_PathIsNotAddedToTheSameHashIfAlreadyExistsInList()
        {
            LocalJsonDatabase localJsonDatabase = new(mJsonSerializer, NullLogger<LocalJsonDatabase>.Instance);
            await localJsonDatabase.Load(Consts.Database.BackupFilesCollectionName, CancellationToken.None);
            FilesHashesHandler filesHashesHandler = new(new List<IBackedUpFilesDatabase> {localJsonDatabase});
            
            const string firstFileName = "FileName.ext";
            const string secondFileName = "FileName2.ext";
            const string hash = "ABC123";
            await filesHashesHandler.AddFileHash(hash, firstFileName, CancellationToken.None);
            await filesHashesHandler.AddFileHash(hash, secondFileName, CancellationToken.None);
            await filesHashesHandler.AddFileHash(hash, firstFileName, CancellationToken.None);
            
            Assert.True(await filesHashesHandler.IsFilePathExist(firstFileName, CancellationToken.None));
            Assert.True(await filesHashesHandler.IsFilePathExist(secondFileName, CancellationToken.None));
            Assert.True(await filesHashesHandler.IsHashExists(hash, CancellationToken.None));
            
            await filesHashesHandler.Save(CancellationToken.None);
            
            Dictionary<string, List<string>> fileHashToFilePathMap =
                await mJsonSerializer.DeserializeAsync<Dictionary<string, List<string>>>(GetBackedUpFilesLocalFilePath(), CancellationToken.None);
            Assert.Equal(2, fileHashToFilePathMap[hash].Count);
        }

        [Fact]
        public async Task IsHashExists_HashAlreadyExists_True()
        {
            LocalJsonDatabase localJsonDatabase = new(mJsonSerializer, NullLogger<LocalJsonDatabase>.Instance);
            await localJsonDatabase.Load(Consts.Database.BackupFilesCollectionName, CancellationToken.None);
            FilesHashesHandler filesHashesHandler = new(new List<IBackedUpFilesDatabase> {localJsonDatabase});

            const string hash = "ABC123";
            await filesHashesHandler.AddFileHash(hash, "fileName", CancellationToken.None);

            Assert.True(await filesHashesHandler.IsHashExists(hash, CancellationToken.None));
        }

        [Fact]
        public async Task IsHashExists_HashNotExists_False()
        {
            LocalJsonDatabase localJsonDatabase = new(mJsonSerializer, NullLogger<LocalJsonDatabase>.Instance);
            await localJsonDatabase.Load(Consts.Database.BackupFilesCollectionName, CancellationToken.None);
            FilesHashesHandler filesHashesHandler = new(new List<IBackedUpFilesDatabase> {localJsonDatabase});

            await filesHashesHandler.AddFileHash("ABC123", "fileName", CancellationToken.None);

            Assert.False(await filesHashesHandler.IsHashExists("ABC1235", CancellationToken.None));
        }

        [Fact]
        public async Task Save_ToFilesWithSameHash_DataFileCreatedAsExpected()
        {
            LocalJsonDatabase localJsonDatabase = new(mJsonSerializer, NullLogger<LocalJsonDatabase>.Instance);
            await localJsonDatabase.Load(Consts.Database.BackupFilesCollectionName, CancellationToken.None);
            FilesHashesHandler filesHashesHandler = new(new List<IBackedUpFilesDatabase> {localJsonDatabase});
            await filesHashesHandler.AddFileHash("expectedHash", "expectedFilePath1", CancellationToken.None);
            await filesHashesHandler.AddFileHash("expectedHash", "expectedFilePath2", CancellationToken.None);
            
            File.Delete(GetBackedUpFilesLocalFilePath());
            
            await filesHashesHandler.Save(CancellationToken.None);
            File.Exists(GetBackedUpFilesLocalFilePath());
            Dictionary<string, List<string>> fileHashToFilePathMap =
                await mJsonSerializer.DeserializeAsync<Dictionary<string, List<string>>>(GetBackedUpFilesLocalFilePath(), CancellationToken.None);

            List<string> files = fileHashToFilePathMap["expectedHash"];
            Assert.Contains("expectedFilePath1", files);
            Assert.Contains("expectedFilePath2", files);
        }
    }
}