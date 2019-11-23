using System.Collections;
using System.Collections.Generic;

namespace BackuperApp
{
    public class DirectoriesBinding : IEnumerable<DirectoriesCouple>
    {
        private readonly List<DirectoriesCouple> mDirectoriesList = new List<DirectoriesCouple>();

        public DirectoriesBinding(List<DirectoriesCouple> directoriesCouples)
        {
            mDirectoriesList = directoriesCouples;
        }

        public IEnumerator<DirectoriesCouple> GetEnumerator()
        {
            return mDirectoriesList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}