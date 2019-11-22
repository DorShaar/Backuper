using System.IO;

namespace BackuperApp
{
    public class DirectoriesCouple
    {
        public string SourceDirectory { get; }
        public string DestDirectory { get; }

        public DirectoriesCouple(string sourceDirectory, string destDirectory)
        {
            if (!Directory.Exists(sourceDirectory))
                throw new DirectoryNotFoundException($"Error in directories registration: {sourceDirectory} does not exists");

            SourceDirectory = sourceDirectory;

            if (!Directory.Exists(destDirectory))
                throw new DirectoryNotFoundException($"Error in directories registration: {destDirectory} does not exists");
            
            DestDirectory = destDirectory;
        }
    }
}