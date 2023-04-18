using System.Threading;

namespace Backuper.App
{
    public interface IBackuperService
    {
        void BackupFiles(CancellationToken cancellationToken);
    }
}