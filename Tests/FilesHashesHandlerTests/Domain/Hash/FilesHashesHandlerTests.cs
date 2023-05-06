using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.Domain.Hash;
using BackupManager.Infra;
using FakeItEasy;
using JsonSerialization;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BackupManagerTests.Domain.Hash
{
    public class FilesHashesHandlerTests : TestsBase
    {
        [Fact]
        public void AddFileHash_FileHashNotExists_FileHashAdded()
        {
            FilesHashesHandler filesHashesHandler = new(A.Dummy<IJsonSerializer>(), NullLogger<FilesHashesHandler>.Instance);

            Assert.Equal(0, filesHashesHandler.HashesCount);

            filesHashesHandler.AddFileHash("ABC123", "FileName.ext");
            filesHashesHandler.AddFileHash("ABC1234", "FileName.ext");
            Assert.Equal(2, filesHashesHandler.HashesCount);
        }

        [Fact]
        public void AddFileHash_FileHashAlreadyExists_PathIsAddedToTheSameHash()
        {
            FilesHashesHandler filesHashesHandler = new(A.Dummy<IJsonSerializer>(), NullLogger<FilesHashesHandler>.Instance);
            
            Assert.Equal(0, filesHashesHandler.HashesCount);
            
            const string firstFileName = "FileName.ext";
            const string secondFileName = "FileName2.ext";
            const string hash = "ABC123";
            filesHashesHandler.AddFileHash(hash, firstFileName);
            filesHashesHandler.AddFileHash(hash, secondFileName);
            
            Assert.Equal(1, filesHashesHandler.HashesCount);

            Assert.True(filesHashesHandler.IsFilePathExist(firstFileName));
            Assert.True(filesHashesHandler.IsFilePathExist(secondFileName));
            Assert.True(filesHashesHandler.IsHashExists(hash));
        }
        
        [Fact]
        public async Task AddFileHash_FileHashAlreadyExists_PathIsNotAddedToTheSameHashIfAlreadyExistsInList()
        {
            FilesHashesHandler filesHashesHandler = new(mJsonSerializer, NullLogger<FilesHashesHandler>.Instance);
            
            Assert.Equal(0, filesHashesHandler.HashesCount);
            
            const string firstFileName = "FileName.ext";
            const string secondFileName = "FileName2.ext";
            const string hash = "ABC123";
            filesHashesHandler.AddFileHash(hash, firstFileName);
            filesHashesHandler.AddFileHash(hash, secondFileName);
            filesHashesHandler.AddFileHash(hash, firstFileName);
            
            Assert.Equal(1, filesHashesHandler.HashesCount);

            Assert.True(filesHashesHandler.IsFilePathExist(firstFileName));
            Assert.True(filesHashesHandler.IsFilePathExist(secondFileName));
            Assert.True(filesHashesHandler.IsHashExists(hash));
            
            await filesHashesHandler.Save(CancellationToken.None).ConfigureAwait(false);
            Dictionary<string, List<string>> fileHashToFilePathMap =
                await mJsonSerializer.DeserializeAsync<Dictionary<string, List<string>>>(Consts.DataFilePath, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(2, fileHashToFilePathMap[hash].Count);
        }

        [Fact]
        public void IsHashExists_HashAlreadyExists_True()
        {
            FilesHashesHandler filesHashesHandler = new(A.Dummy<IJsonSerializer>(), NullLogger<FilesHashesHandler>.Instance);

            const string hash = "ABC123";
            filesHashesHandler.AddFileHash(hash, "fileName");

            Assert.True(filesHashesHandler.IsHashExists(hash));
        }

        [Fact]
        public void IsHashExists_HashNotExists_False()
        {
            FilesHashesHandler filesHashesHandler = new(A.Dummy<IJsonSerializer>(), NullLogger<FilesHashesHandler>.Instance);

            filesHashesHandler.AddFileHash("ABC123", "fileName");

            Assert.False(filesHashesHandler.IsHashExists("ABC1235"));
        }

        [Fact]
        public async Task Save_ToFilesWithSameHash_DataFileCreatedAsExpected()
        {
            FilesHashesHandler filesHashesHandler = new(mJsonSerializer, NullLogger<FilesHashesHandler>.Instance);
            filesHashesHandler.AddFileHash("expectedHash", "expectedFilePath1");
            filesHashesHandler.AddFileHash("expectedHash", "expectedFilePath2");
            
            File.Delete(Consts.DataFilePath);
            
            await filesHashesHandler.Save(CancellationToken.None).ConfigureAwait(false);
            File.Exists(Consts.DataFilePath);
            Dictionary<string, List<string>> fileHashToFilePathMap =
                await mJsonSerializer.DeserializeAsync<Dictionary<string, List<string>>>(Consts.DataFilePath, CancellationToken.None).ConfigureAwait(false);

            List<string> files = fileHashToFilePathMap["expectedHash"];
            Assert.Contains("expectedFilePath1", files);
            Assert.Contains("expectedFilePath2", files);
        }
    }
}