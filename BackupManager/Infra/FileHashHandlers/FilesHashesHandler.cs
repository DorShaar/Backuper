using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.App.Database;
using BackupManager.Infra.DB.Models;
using BackupManagerCore.Hash;

namespace BackupManager.Infra.FileHashHandlers
{
    public class FilesHashesHandler : IFilesHashesHandler
    {
        private readonly List<IBackedUpFilesDatabase> mDatabases;
        
        public FilesHashesHandler(List<IBackedUpFilesDatabase> databases)
        {
            if (databases is null || databases.Count == 0)
            {
                throw new ArgumentNullException(nameof(databases));
            }

            mDatabases = databases;
        }

        public async Task LoadDatabase(string databaseName, CancellationToken cancellationToken)
        {
            foreach (IBackedUpFilesDatabase database in mDatabases)
            {
                await database.Load(databaseName, cancellationToken).ConfigureAwait(false);    
            }
        }

        public async Task<bool> IsHashExists(string hash, CancellationToken cancellationToken)
        {
            BackedUpFileSearchModel searchModel = new()
            {
                FileHash = hash
            };
            
            IEnumerable<BackedUpFile>? backedUpFiles = await mDatabases[0].Find(searchModel, cancellationToken);
            return backedUpFiles is not null && backedUpFiles.Any();
        }

        public async Task<bool> IsFilePathExist(string filePath, CancellationToken cancellationToken)
        {
            BackedUpFileSearchModel searchModel = new()
            {
                FilePath = filePath
            };
            
            IEnumerable<BackedUpFile>? backedUpFiles = await mDatabases[0].Find(searchModel, cancellationToken);
            return backedUpFiles is not null && backedUpFiles.Any();
        }

        public string CalculateHash(string filePath) => HashCalculator.CalculateHash(filePath);

        public async Task AddFileHash(string fileHash, string filePath, CancellationToken cancellationToken)
        {
            BackedUpFile backedUpFile = new()
            {
                FileHash = fileHash,
                FilePath = filePath
            };

            foreach (IBackedUpFilesDatabase database in mDatabases)
            {
                await database.Insert(backedUpFile, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task Save(CancellationToken cancellationToken)
        {
            foreach (IBackedUpFilesDatabase database in mDatabases)
            {
                await database.Save(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}