using System;

namespace BackupManager.Infra.DB.Models;

public class BackedUpFile
{
	public string Id { get; } = Guid.NewGuid().ToString();

	public required string FilePath { get; init; }
	
	public required string FileHash { get; init; }
	
	public string? BackupTime { get; set; }
}