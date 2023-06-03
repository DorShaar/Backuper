namespace BackupManager.Domain.Mapping
{
    public class DirectoriesMap
    {
        public required string SourceRelativeDirectory { get; set; }
        
        public required string? DestRelativeDirectory { get; init; }
    }
}