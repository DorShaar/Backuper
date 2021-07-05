using Backuper.Domain.Mapping;
using Backuper.Infra;
using System;

namespace Backuper.App
{
    public interface IBackuperService
    {
        void BackupFiles();
    }
}