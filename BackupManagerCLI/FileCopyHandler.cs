using IOWrapper;

namespace BackupManagerCli
{
    internal static class FileCopyHandler
    {
        public static async Task Handle(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Please provide source directory, destination directory and file that contains relative files");
                return;
            }

            string sourceDirectory = args[0];
            string destinationDirectory = args[1];
            string relativePathsFile = args[2];

            if (!File.Exists(relativePathsFile))
            {
                Console.WriteLine($"File '{relativePathsFile}' does not exist");
                return;
            }

            await CopyFiles(new FileSystemPath(sourceDirectory),
                new FileSystemPath(destinationDirectory),
                relativePathsFile).ConfigureAwait(false);
        }

        private static async Task CopyFiles(FileSystemPath sourceDirectory,
                                            FileSystemPath destinationDirectory,
                                            string relativePathsFile)
        {
            string[] filesToCopy = await File.ReadAllLinesAsync(relativePathsFile);

            foreach (string relativeFilePath in filesToCopy)
            {
                FileSystemPath sourceFilePath = sourceDirectory.Combine(relativeFilePath);
                FileSystemPath destinationFilePath = destinationDirectory.Combine(relativeFilePath);

                if (File.Exists(destinationFilePath.PathString))
                {
                    Console.WriteLine($"File {destinationDirectory.PathString} already exist");
                    continue;
                }

                string parentDirectory = Path.GetDirectoryName(destinationFilePath.PathString) 
                    ?? throw new Exception($"{destinationFilePath.PathString} has no parent directory");
                Directory.CreateDirectory(parentDirectory);

                Console.WriteLine($"Copying file {sourceFilePath.PathString} to {destinationFilePath.PathString}");
                File.Copy(sourceFilePath.PathString, destinationFilePath.PathString);
            }
        }
    }
}
