using FileHashes;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FilesHashesHandlerTests
{
    public class FileHashesHandlerTests
    {
        [Fact]
        public void AddFileHash_NewHashIsAdded()
        {
            FilesHashesHandler hashesHandler = new FilesHashesHandler();
            Assert.Equal(0, hashesHandler.Count);

            hashesHandler.AddFileHash("ABC123", "FileName.ext");
            Assert.Equal(1, hashesHandler.Count);
        }

        [Fact]
        public void AddFileHash_ExitingHashAndPathIsAdded()
        {
            FilesHashesHandler hashesHandler = new FilesHashesHandler();
            Assert.Equal(0, hashesHandler.Count);

            string firstFileName = "FileName.ext";
            string secondFileName = "FileName2.ext";
            hashesHandler.AddFileHash("ABC123", firstFileName);
            hashesHandler.AddFileHash("ABC123", secondFileName);

            Assert.Equal(1, hashesHandler.Count);

            KeyValuePair<string, List<string>> pair = hashesHandler.ToList().First();

            Assert.Equal(firstFileName, pair.Value[0]);
            Assert.Equal(secondFileName, pair.Value[1]);
        }
    }
}