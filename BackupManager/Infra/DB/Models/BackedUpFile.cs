namespace BackupManager.Infra.DB.Models;

public class BackedUpFile
{
	public string? Id { get; init; }

	public required string FilePath { get; init; }
	
	public required string FileHash { get; init; }
	
	public string? BackupTime { get; init; }
}