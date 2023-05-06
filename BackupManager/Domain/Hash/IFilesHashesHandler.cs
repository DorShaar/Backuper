using System.Threading;
using System.Threading.Tasks;

namespace BackupManager.Domain.Hash;

public interface IFilesHashesHandler
{
    int HashesCount { get; }

    bool IsHashExists(string hash);

    bool IsFilePathExist(string filePath);

    string CalculateHash(string filePath);

    void AddFileHash(string fileHash, string filePath);

    Task Save(CancellationToken cancellationToken);
}
