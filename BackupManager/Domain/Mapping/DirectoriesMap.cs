namespace Backuper.Domain.Mapping
{
    public class DirectoriesMap
    {
        // TOdO DOR add test serialization is working.
        public required string SourceRelativeDirectory { get; init; }
        
        public required string DestRelativeDirectory { get; init; }
    }
}