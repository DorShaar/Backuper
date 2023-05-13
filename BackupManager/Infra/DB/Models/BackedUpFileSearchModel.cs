namespace BackupManager.Infra.DB.Models;

public class BackedUpFileSearchModel
{
	public string? Id { get; init; }

	public string? FilePath { get; init; }
	
	public string? FileHash { get; init; }
	
	public string? BackupTime { get; init; }
}