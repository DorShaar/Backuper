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
            mSourceDirectory = sourceDirectory;
        }

        public void SetDestinationDirectory(string destDirectory)
        {
            mDestDirectory = destDirectory;
        }
    }
}