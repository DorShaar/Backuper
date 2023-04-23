namespace BackupManager.Domain.Mapping
{
    public class DirectoriesMap
    {
        public required string SourceRelativeDirectory { get; init; }
        
        public required string DestRelativeDirectory { get; set; }
    }
}