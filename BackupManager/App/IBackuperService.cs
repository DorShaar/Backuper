using System.Threading;
using System.Threading.Tasks;

namespace Backuper.App
{
    public interface IBackuperService
    {
        void BackupFiles(CancellationToken cancellationToken);
    }
}