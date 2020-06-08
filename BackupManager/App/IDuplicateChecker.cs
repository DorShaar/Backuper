using System.Collections.Generic;

namespace Backuper.App
{
    public interface IDuplicateChecker
    {
        void WriteDuplicateFiles(string rootDirectory, string duplicatesFilesTxtFile);
        Dictionary<string, List<string>> FindDuplicateFiles(string rootDirectory);
    }
}