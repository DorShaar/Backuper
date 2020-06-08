using Backuper.Infra.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace BackupManagerTests.Infra
{
    public class JsonSerializerWrapperTests
    {
        [Fact]
        public void SerializeAndDeserialize_Success()
        {
            JsonSerializerWrapper jsonSerializer = new JsonSerializerWrapper();

            Dictionary<string, List<string>> HashToFilePathDict =
                new Dictionary<string, List<string>>
                {
                    { "abc", new List<string> { "123", "456" } },
                    { "def", new List<string> { "789", "456" } }
                };

            string tempFile = Guid.NewGuid().ToString();

            try
            {
                jsonSerializer.Serialize(HashToFilePathDict, tempFile);
                Dictionary<string, List<string>> testedHashToFilePathDict =
                    jsonSerializer.Deserialize<Dictionary<string, List<string>>>(tempFile);

                Assert.Equal(HashToFilePathDict["abc"][0], testedHashToFilePathDict["abc"][0]);
                Assert.Equal(HashToFilePathDict["abc"][1], testedHashToFilePathDict["abc"][1]);
                Assert.Equal(HashToFilePathDict["def"][0], testedHashToFilePathDict["def"][0]);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}