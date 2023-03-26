namespace Backuper.Domain.Mapping
{
    public class DirectoriesMap
    {
        // TOdO DOR add test serialization is working.
        public required string SourceDirectory { get; init; }
        public required string DestDirectory { get; init; }

        // TODO DOR add test
        public string GetNewFilePath(string filePath)
        {
            return filePath.Replace(SourceDirectory, DestDirectory);
        }
    }
}