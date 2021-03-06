﻿using System.Collections;
using System.Collections.Generic;

namespace Backuper.Domain.Mapping
{
    public class DirectoriesMapping : IEnumerable<DirectoriesMap>
    {
        private readonly List<DirectoriesMap> mDirectoriesList = new List<DirectoriesMap>();

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