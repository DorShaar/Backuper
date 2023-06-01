using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BackupManager.App.Database;

public interface IDatabase<TItem, in TSearchModelType>
{
	Task Load(string databaseName, CancellationToken cancellationToken);
	
	Task<IEnumerable<TItem>> GetAll(CancellationToken cancellationToken);
	
	Task Insert(TItem itemToInsert, CancellationToken cancellationToken);

	Task Save(CancellationToken cancellationToken);

	Task<IEnumerable<TItem>?> Find(TSearchModelType searchParameter, CancellationToken cancellationToken);
}