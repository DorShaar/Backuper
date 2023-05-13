using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.App.Database;
using BackupManager.Domain.Hash;
using BackupManager.Infra.DB.Models;
using Microsoft.Extensions.Logging;

namespace BackupManager.Infra.FileHashHandlers
{
    public class FilesHashesHandler : IFilesHashesHandler
    {
        private readonly IBackedUpFilesDatabase mDatabase;
        private readonly ILogger<FilesHashesHandler> mLogger;
        
        public FilesHashesHandler(IBackedUpFilesDatabase database, ILogger<FilesHashesHandler> logger)
        {
            mDatabase = database ?? throw new ArgumentNullException(nameof(database));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> IsHashExists(string hash, CancellationToken cancellationToken)
        {
            BackedUpFileSearchModel searchModel = new()
            {
                FileHash = hash
            };
            
            IEnumerable<BackedUpFile>? backedUpFiles = await mDatabase.Find(searchModel, cancellationToken);
            return backedUpFiles is not null && backedUpFiles.Any();
        }

        public async Task<bool> IsFilePathExist(string filePath, CancellationToken cancellationToken)
        {
            BackedUpFileSearchModel searchModel = new()
            {
                FilePath = filePath
            };
            
            IEnumerable<BackedUpFile>? backedUpFiles = await mDatabase.Find(searchModel, cancellationToken);
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

            await mDatabase.Insert(backedUpFile, cancellationToken).ConfigureAwait(false);
        }

        public async Task Save(CancellationToken cancellationToken)
        {
            await mDatabase.Save(cancellationToken).ConfigureAwait(false);
        }
    }
}