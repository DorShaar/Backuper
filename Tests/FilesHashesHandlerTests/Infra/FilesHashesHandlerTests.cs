using Backuper.App;
using Backuper.App.Serialization;
using Backuper.Domain.Configuration;
using Backuper.Infra;
using BackupManager.Infra;
using FakeItEasy;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace BackupManagerTests.Infra
{
    public class FilesHashesHandlerTests
    {
        private readonly IOptions<BackuperConfiguration> mConfiguration = A.Fake<IOptions<BackuperConfiguration>>();

        public FilesHashesHandlerTests()
        {
            mConfiguration.Value.DriveRootDirectory = "root";
        }

        [Fact]
        public void AddFileHash_FileHashNotExists_FileHashAdded()
        {
            FilesHashesHandler filesHashesHandler = new FilesHashesHandler(
                A.Dummy<IDuplicateChecker>(),
                A.Dummy<IObjectSerializer>(),
                new UnregisteredHashesAdder(mConfiguration),
                mConfiguration);

            Assert.Equal(0, filesHashesHandler.HashesCount);

            filesHashesHandler.AddFileHash("ABC123", "FileName.ext");
            filesHashesHandler.AddFileHash("ABC1234", "FileName.ext");
            Assert.Equal(2, filesHashesHandler.HashesCount);
        }

        [Fact]
        public void AddFileHash_FileHashAlreadyExists_PathIsAdded()
        {
            FilesHashesHandler filesHashesHandler = new FilesHashesHandler(
                A.Dummy<IDuplicateChecker>(),
                A.Dummy<IObjectSerializer>(),
                new UnregisteredHashesAdder(mConfiguration),
                mConfiguration);

            Assert.Equal(0, filesHashesHandler.HashesCount);

            const string firstFileName = "FileName.ext";
            const string secondFileName = "FileName2.ext";
            filesHashesHandler.AddFileHash("ABC123", firstFileName);
            filesHashesHandler.AddFileHash("ABC123", secondFileName);

            Assert.Equal(1, filesHashesHandler.HashesCount);

            KeyValuePair<string, List<string>> pair = filesHashesHandler.HashToFilePathDict.First();

            Assert.Equal(firstFileName, pair.Value[0]);
            Assert.Equal(secondFileName, pair.Value[1]);
        }

        [Fact]
        public void HashExists_HashAlreadyExists_True()
        {
            FilesHashesHandler filesHashesHandler = new FilesHashesHandler(
                A.Dummy<IDuplicateChecker>(),
                A.Dummy<IObjectSerializer>(),
                new UnregisteredHashesAdder(mConfiguration),
                mConfiguration);

            const string hash = "ABC123";
            filesHashesHandler.AddFileHash(hash, "fileName");

            Assert.True(filesHashesHandler.HashExists(hash));
        }

        [Fact]
        public void HashExists_HashNotExists_False()
        {
            FilesHashesHandler filesHashesHandler = new FilesHashesHandler(
                A.Dummy<IDuplicateChecker>(),
                A.Dummy<IObjectSerializer>(),
                new UnregisteredHashesAdder(mConfiguration),
                mConfiguration);

            filesHashesHandler.AddFileHash("ABC123", "fileName");

            Assert.False(filesHashesHandler.HashExists("ABC1235"));
        }

        [Fact]
        public void Ctor_DeserializeCalledForLoadingDB()
        {
            IObjectSerializer objectSerializer = A.Fake<IObjectSerializer>();

            FilesHashesHandler filesHashesHandler = new FilesHashesHandler(
                A.Dummy<IDuplicateChecker>(),
                objectSerializer,
                new UnregisteredHashesAdder(mConfiguration),
                mConfiguration);

            A.CallTo(() => objectSerializer.Deserialize<Dictionary<string, List<string>>>(
                Path.Combine(mConfiguration.Value.DriveRootDirectory, "hashes.txt")))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void WriteHashesFiles_FileCreated()
        {
            IObjectSerializer objectSerializer = A.Fake<IObjectSerializer>();

            FilesHashesHandler filesHashesHandler = new FilesHashesHandler(
                A.Fake<IDuplicateChecker>(),
                objectSerializer,
                new UnregisteredHashesAdder(mConfiguration),
                mConfiguration);

            filesHashesHandler.WriteHashesFiles();
            A.CallTo(() => objectSerializer.Serialize(
                A<Dictionary<string, List<string>>>.Ignored,
                Path.Combine(mConfiguration.Value.DriveRootDirectory, "hashes.txt")))
                .MustHaveHappened();
        }
    }
}