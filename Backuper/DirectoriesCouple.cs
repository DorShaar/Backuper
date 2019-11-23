using System.IO;

namespace BackuperApp
{
    public class DirectoriesCouple
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
                throw new DirectoryNotFoundException($"Error in directories registration: {sourceDirectory} does not exists");

            mSourceDirectory = sourceDirectory;
        }

        public void SetDestinationDirectory(string destDirectory)
        {
            if (!Directory.Exists(destDirectory))
                throw new DirectoryNotFoundException($"Error in directories registration: {destDirectory} does not exists");

            mDestDirectory = destDirectory;
        }
    }
}