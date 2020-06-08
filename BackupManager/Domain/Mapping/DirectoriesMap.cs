using System.IO;

namespace Backuper.Domain.Mapping
{
    public class DirectoriesMap
    {
        private string mSourceDirectory;
        public string SourceDirectory
        {
            get => mSourceDirectory;
            set => SetSourceDirectory(value);
        }

        private string mDestDirectory;
        public string DestDirectory
        {
            get => mDestDirectory;
            set => SetDestinationDirectory(value);
        }

        public void SetSourceDirectory(string sourceDirectory)
        {
            if (!Directory.Exists(sourceDirectory))
                System.Console.WriteLine($"Error in directories registration: {sourceDirectory} does not exists");

            mSourceDirectory = sourceDirectory;
        }

        public void SetDestinationDirectory(string destDirectory)
        {
            if (!Directory.Exists(destDirectory))
                System.Console.WriteLine($"Error in directories registration: {destDirectory} does not exists");

            mDestDirectory = destDirectory;
        }
    }
}