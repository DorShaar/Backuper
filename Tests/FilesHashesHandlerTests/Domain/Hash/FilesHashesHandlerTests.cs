using System.Collections.Generic;
using System.IO;
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
        public void HashExists_HashAlreadyExists_True()
        {
            FilesHashesHandler filesHashesHandler = new(A.Dummy<IJsonSerializer>(), NullLogger<FilesHashesHandler>.Instance);

            const string hash = "ABC123";
            filesHashesHandler.AddFileHash(hash, "fileName");

            Assert.True(filesHashesHandler.IsHashExists(hash));
        }

        [Fact]
        public void HashExists_HashNotExists_False()
        {
            FilesHashesHandler filesHashesHandler = new(A.Dummy<IJsonSerializer>(), NullLogger<FilesHashesHandler>.Instance);

            filesHashesHandler.AddFileHash("ABC123", "fileName");

            Assert.False(filesHashesHandler.IsHashExists("ABC1235"));
        }

        [Fact]
        public void Save_ToFilesWithSameHash_DataFileCreatedAsExpected()
        {
            IJsonSerializer jsonSerializer = new JsonSerializer();
            
            FilesHashesHandler filesHashesHandler = new(jsonSerializer, NullLogger<FilesHashesHandler>.Instance);
            filesHashesHandler.AddFileHash("expectedHash", "expectedFilePath1");
            filesHashesHandler.AddFileHash("expectedHash", "expectedFilePath2");
            
            File.Delete(Consts.DataFilePath);
            
            filesHashesHandler.Save();
            File.Exists(Consts.DataFilePath);
            Dictionary<string, List<string>> fileHashToFilePathMap = jsonSerializer.Deserialize<Dictionary<string, List<string>>>(Consts.DataFilePath);

            List<string> files = fileHashToFilePathMap["expectedHash"];
            Assert.Contains("expectedFilePath1", files);
            Assert.Contains("expectedFilePath2", files);
        }
    }
}