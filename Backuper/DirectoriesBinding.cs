using System.Collections;
using System.Collections.Generic;

namespace BackuperApp
{
    public class DirectoriesBinding : IEnumerable<DirectoriesCouple>
    {
        private readonly List<DirectoriesCouple> mDirectoriesList = new List<DirectoriesCouple>();

        public void Register(string backupFromDirectory, string backupToDirectory)
        {
            mDirectoriesList.Add(new DirectoriesCouple(backupFromDirectory, backupToDirectory));
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