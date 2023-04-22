using System.Collections;
using System.Collections.Generic;

namespace BackupManager.Domain.Mapping
{
    public class DirectoriesMapping : IEnumerable<DirectoriesMap>
    {
        private readonly List<DirectoriesMap> mDirectoriesList;

        public DirectoriesMapping(List<DirectoriesMap> directoriesCouples)
        {
            mDirectoriesList = directoriesCouples;
        }

        public IEnumerator<DirectoriesMap> GetEnumerator()
        {
            return mDirectoriesList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}