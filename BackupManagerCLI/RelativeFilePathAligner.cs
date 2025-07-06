using IOWrapper;
using JsonSerialization;

namespace BackupManagerCli
{
    internal static class RelativeFilePathAligner
    {
        private static readonly IJsonSerializer _jsonSerializer = new JsonSerializer();

        public static async Task Align(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Please provide relative-to-path and hashes map file path");
                return;
            }

            string relativeToPath = args[0];

            string hashmapFilePath = args[1];
            if (!File.Exists(hashmapFilePath))
            {
                Console.WriteLine($"File '{hashmapFilePath}' does not exist");
                return;
            }

            Dictionary<string, List<string>> hashToFilePaths =
                await _jsonSerializer.DeserializeAsync<Dictionary<string, List<string>>>(hashmapFilePath, CancellationToken.None)
                .ConfigureAwait(false);

            Dictionary<string, List<string>> aligned = Convert(hashToFilePaths, relativeToPath);
            await _jsonSerializer.SerializeAsync(aligned, hashmapFilePath, CancellationToken.None)
                .ConfigureAwait(false);
        }

        private static Dictionary<string, List<string>> Convert(Dictionary<string, List<string>> hashToFilePaths,
            string relativeToPath)
        {
            Dictionary<string, List<string>> alignedHashToFilePaths = [];

            foreach ((string hash, List<string> filePaths) in hashToFilePaths)
            {
                List<string> aligned = filePaths.Select(filePath => (new FileSystemPath(filePath)).GetRelativePath(relativeToPath).PathString)
                                                        .ToList();
                alignedHashToFilePaths.Add(hash, aligned);
            }

            return alignedHashToFilePaths;
        }
    }
}
