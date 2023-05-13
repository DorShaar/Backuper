using System.Threading;
using System.Threading.Tasks;

namespace BackupManager.App.Database;

public interface IFilesHashesHandler
{
    Task<bool> IsHashExists(string hash, CancellationToken cancellationToken);

    Task<bool> IsFilePathExist(string filePath, CancellationToken cancellationToken);

    string CalculateHash(string filePath);

    Task AddFileHash(string fileHash, string filePath, CancellationToken cancellationToken);

    Task Save(CancellationToken cancellationToken);
}
