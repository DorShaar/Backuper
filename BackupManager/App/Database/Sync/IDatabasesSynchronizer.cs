using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BackupManager.App.Database.Sync;

public interface IDatabasesSynchronizer
{
	Task SyncDatabases(IEnumerable<string> knownTokens, CancellationToken cancellationToken);
}