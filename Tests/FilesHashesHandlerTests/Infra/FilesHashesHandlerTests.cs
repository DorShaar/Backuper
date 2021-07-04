using Backuper.App;
using Backuper.App.Serialization;
using Backuper.Domain.Configuration;
using Backuper.Infra;
using FakeItEasy;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BackupManagerTests.Infra
{
    public class FilesHashesHandlerTests
    {
        [Fact]
        public void AddFileHash_FileHashNotExists_FileHashAdded()
        {
            FilesHashesHandler filesHashesHandler = new FilesHashesHandler(
                A.Dummy<IDuplicateChecker>(),
                A.Dummy<IObjectSerializer>(),
                A.Dummy<IOptions<BackuperConfiguration>>());

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
                A.Dummy<IOptions<BackuperConfiguration>>());

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
                A.Dummy<IOptions<BackuperConfiguration>>());

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
                A.Dummy<IOptions<BackuperConfiguration>>());

            filesHashesHandler.AddFileHash("ABC123", "fileName");

            Assert.False(filesHashesHandler.HashExists("ABC1235"));
        }

        [Fact]
        public void Ctor_DeserializeCalledForLoadingDB()
        {
            IObjectSerializer objectSerializer = A.Fake<IObjectSerializer>();

            IOptions<BackuperConfiguration> config = A.Fake<IOptions<BackuperConfiguration>>();
            config.Value.FileHashesPath = "filePath";

            FilesHashesHandler filesHashesHandler = new FilesHashesHandler(
                A.Dummy<IDuplicateChecker>(),
                objectSerializer,
                config);

            A.CallTo(() => objectSerializer.Deserialize<Dictionary<string, List<string>>>(config.Value.FileHashesPath))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void Save_FileCreated()
        {
            IObjectSerializer objectSerializer = A.Fake<IObjectSerializer>();

            IOptions<BackuperConfiguration> config = A.Fake<IOptions<BackuperConfiguration>>();
            config.Value.FileHashesPath = "filePath";

            FilesHashesHandler filesHashesHandler = new FilesHashesHandler(
                A.Fake<IDuplicateChecker>(),
                objectSerializer,
                config);

            filesHashesHandler.Save();
            A.CallTo(() => objectSerializer.Serialize(
                A<Dictionary<string, List<string>>>.Ignored, config.Value.FileHashesPath))
                .MustHaveHappened();
        }
    }
}