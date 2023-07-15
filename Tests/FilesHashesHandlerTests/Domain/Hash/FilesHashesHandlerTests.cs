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
            await localJsonDatabase.Load(Consts.BackupFilesCollectionName, CancellationToken.None).ConfigureAwait(false);
            FilesHashesHandler filesHashesHandler = new(new List<IBackedUpFilesDatabase> {localJsonDatabase});

            await filesHashesHandler.AddFileHash("ABC123", "FileName.ext", CancellationToken.None).ConfigureAwait(false);
            await filesHashesHandler.AddFileHash("ABC1234", "FileName.ext", CancellationToken.None).ConfigureAwait(false);
            
            Assert.True(await filesHashesHandler.IsHashExists("ABC123", CancellationToken.None).ConfigureAwait(false));
            Assert.True(await filesHashesHandler.IsHashExists("ABC1234", CancellationToken.None).ConfigureAwait(false));
            Assert.True(await filesHashesHandler.IsFilePathExist("FileName.ext", CancellationToken.None).ConfigureAwait(false));
        }

        [Fact]
        public async Task AddFileHash_FileHashAlreadyExists_PathIsAddedToTheSameHash()
        {
            LocalJsonDatabase localJsonDatabase = new(mJsonSerializer, NullLogger<LocalJsonDatabase>.Instance);
            await localJsonDatabase.Load(Consts.BackupFilesCollectionName, CancellationToken.None).ConfigureAwait(false);
            FilesHashesHandler filesHashesHandler = new(new List<IBackedUpFilesDatabase> {localJsonDatabase});
            
            const string firstFileName = "FileName.ext";
            const string secondFileName = "FileName2.ext";
            const string hash = "ABC123";
            await filesHashesHandler.AddFileHash(hash, firstFileName, CancellationToken.None).ConfigureAwait(false);
            await filesHashesHandler.AddFileHash(hash, secondFileName, CancellationToken.None).ConfigureAwait(false);
            
            Assert.True(await filesHashesHandler.IsFilePathExist(firstFileName, CancellationToken.None).ConfigureAwait(false));
            Assert.True(await filesHashesHandler.IsFilePathExist(secondFileName, CancellationToken.None).ConfigureAwait(false));
            Assert.True(await filesHashesHandler.IsHashExists(hash, CancellationToken.None).ConfigureAwait(false));
        }
        
        [Fact]
        public async Task AddFileHash_FileHashAlreadyExists_PathIsNotAddedToTheSameHashIfAlreadyExistsInList()
        {
            LocalJsonDatabase localJsonDatabase = new(mJsonSerializer, NullLogger<LocalJsonDatabase>.Instance);
            await localJsonDatabase.Load(Consts.BackupFilesCollectionName, CancellationToken.None).ConfigureAwait(false);
            FilesHashesHandler filesHashesHandler = new(new List<IBackedUpFilesDatabase> {localJsonDatabase});
            
            const string firstFileName = "FileName.ext";
            const string secondFileName = "FileName2.ext";
            const string hash = "ABC123";
            await filesHashesHandler.AddFileHash(hash, firstFileName, CancellationToken.None).ConfigureAwait(false);
            await filesHashesHandler.AddFileHash(hash, secondFileName, CancellationToken.None).ConfigureAwait(false);
            await filesHashesHandler.AddFileHash(hash, firstFileName, CancellationToken.None).ConfigureAwait(false);
            
            Assert.True(await filesHashesHandler.IsFilePathExist(firstFileName, CancellationToken.None).ConfigureAwait(false));
            Assert.True(await filesHashesHandler.IsFilePathExist(secondFileName, CancellationToken.None).ConfigureAwait(false));
            Assert.True(await filesHashesHandler.IsHashExists(hash, CancellationToken.None).ConfigureAwait(false));
            
            await filesHashesHandler.Save(CancellationToken.None).ConfigureAwait(false);
            
            Dictionary<string, List<string>> fileHashToFilePathMap =
                await mJsonSerializer.DeserializeAsync<Dictionary<string, List<string>>>(GetBackedUpFilesLocalFilePath(), CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(2, fileHashToFilePathMap[hash].Count);
        }

        [Fact]
        public async Task IsHashExists_HashAlreadyExists_True()
        {
            LocalJsonDatabase localJsonDatabase = new(mJsonSerializer, NullLogger<LocalJsonDatabase>.Instance);
            await localJsonDatabase.Load(Consts.BackupFilesCollectionName, CancellationToken.None).ConfigureAwait(false);
            FilesHashesHandler filesHashesHandler = new(new List<IBackedUpFilesDatabase> {localJsonDatabase});

            const string hash = "ABC123";
            await filesHashesHandler.AddFileHash(hash, "fileName", CancellationToken.None).ConfigureAwait(false);

            Assert.True(await filesHashesHandler.IsHashExists(hash, CancellationToken.None).ConfigureAwait(false));
        }

        [Fact]
        public async Task IsHashExists_HashNotExists_False()
        {
            LocalJsonDatabase localJsonDatabase = new(mJsonSerializer, NullLogger<LocalJsonDatabase>.Instance);
            await localJsonDatabase.Load(Consts.BackupFilesCollectionName, CancellationToken.None).ConfigureAwait(false);
            FilesHashesHandler filesHashesHandler = new(new List<IBackedUpFilesDatabase> {localJsonDatabase});

            await filesHashesHandler.AddFileHash("ABC123", "fileName", CancellationToken.None).ConfigureAwait(false);

            Assert.False(await filesHashesHandler.IsHashExists("ABC1235", CancellationToken.None).ConfigureAwait(false));
        }

        [Fact]
        public async Task Save_ToFilesWithSameHash_DataFileCreatedAsExpected()
        {
            LocalJsonDatabase localJsonDatabase = new(mJsonSerializer, NullLogger<LocalJsonDatabase>.Instance);
            await localJsonDatabase.Load(Consts.BackupFilesCollectionName, CancellationToken.None).ConfigureAwait(false);
            FilesHashesHandler filesHashesHandler = new(new List<IBackedUpFilesDatabase> {localJsonDatabase});
            await filesHashesHandler.AddFileHash("expectedHash", "expectedFilePath1", CancellationToken.None).ConfigureAwait(false);
            await filesHashesHandler.AddFileHash("expectedHash", "expectedFilePath2", CancellationToken.None).ConfigureAwait(false);
            
            File.Delete(GetBackedUpFilesLocalFilePath());
            
            await filesHashesHandler.Save(CancellationToken.None).ConfigureAwait(false);
            File.Exists(GetBackedUpFilesLocalFilePath());
            Dictionary<string, List<string>> fileHashToFilePathMap =
                await mJsonSerializer.DeserializeAsync<Dictionary<string, List<string>>>(GetBackedUpFilesLocalFilePath(), CancellationToken.None).ConfigureAwait(false);

            List<string> files = fileHashToFilePathMap["expectedHash"];
            Assert.Contains("expectedFilePath1", files);
            Assert.Contains("expectedFilePath2", files);
        }
    }
}