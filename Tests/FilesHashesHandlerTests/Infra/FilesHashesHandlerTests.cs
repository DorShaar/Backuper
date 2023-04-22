using BackupManager.App.Serialization;
using BackupManager.Infra;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BackupManagerTests.Infra
{
    public class FilesHashesHandlerTests
    {
        [Fact]
        public void AddFileHash_FileHashNotExists_FileHashAdded()
        {
            FilesHashesHandler filesHashesHandler = new(A.Dummy<IObjectSerializer>(), NullLogger<FilesHashesHandler>.Instance);

            Assert.Equal(0, filesHashesHandler.HashesCount);

            filesHashesHandler.AddFileHash("ABC123", "FileName.ext");
            filesHashesHandler.AddFileHash("ABC1234", "FileName.ext");
            Assert.Equal(2, filesHashesHandler.HashesCount);
        }

        [Fact]
        public void AddFileHash_FileHashAlreadyExists_PathIsAdded()
        {
            Assert.Fail("TODO DOR");
            
            // FilesHashesHandler filesHashesHandler = new(
            //     A.Dummy<IObjectSerializer>(),
            //     mConfiguration,
            //     NullLogger<FilesHashesHandler>.Instance);
            //
            // Assert.Equal(0, filesHashesHandler.HashesCount);
            //
            // const string firstFileName = "FileName.ext";
            // const string secondFileName = "FileName2.ext";
            // filesHashesHandler.AddFileHash("ABC123", firstFileName);
            // filesHashesHandler.AddFileHash("ABC123", secondFileName);
            //
            // Assert.Equal(1, filesHashesHandler.HashesCount);
            //
            // KeyValuePair<string, List<string>> pair = filesHashesHandler.HashToFilePathDict.First();
            //
            // Assert.Equal(firstFileName, pair.Value[0]);
            // Assert.Equal(secondFileName, pair.Value[1]);
        }

        [Fact]
        public void HashExists_HashAlreadyExists_True()
        {
            FilesHashesHandler filesHashesHandler = new(A.Dummy<IObjectSerializer>(), NullLogger<FilesHashesHandler>.Instance);

            const string hash = "ABC123";
            filesHashesHandler.AddFileHash(hash, "fileName");

            Assert.True(filesHashesHandler.HashExists(hash));
        }

        [Fact]
        public void HashExists_HashNotExists_False()
        {
            FilesHashesHandler filesHashesHandler = new(A.Dummy<IObjectSerializer>(), NullLogger<FilesHashesHandler>.Instance);

            filesHashesHandler.AddFileHash("ABC123", "fileName");

            Assert.False(filesHashesHandler.HashExists("ABC1235"));
        }

        [Fact]
        public void WriteHashesFiles_FileCreated()
        {
            Assert.Fail("TODO DOR");
            
            // IObjectSerializer objectSerializer = A.Fake<IObjectSerializer>();
            //
            // FilesHashesHandler filesHashesHandler = new(
            //     objectSerializer,
            //     mConfiguration,
            //     NullLogger<FilesHashesHandler>.Instance);
            //
            // filesHashesHandler.WriteHashesFiles();
            // A.CallTo(() => objectSerializer.Serialize(
            //     A<Dictionary<string, List<string>>>.Ignored,
            //     Path.Combine(mConfiguration.Value.RootDirectory, "hashes.txt")))
            //     .MustHaveHappened();
        }
    }
}