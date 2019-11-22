using System;
using System.Collections.Generic;
using System.IO;

namespace RecursiveDirectoryEnumaratorClass
{
    public class RecursiveDirectoryEnumarator<T> where T : new()
    {
        public T OperateRecursive(
            string rootDirectory, 
            Action<string, T> fileOperation, 
            string operationName = "")
        {
            T returnValue = new T();

            if (!Directory.Exists(rootDirectory))
            {
                Console.WriteLine($"{rootDirectory} does not exists");
                return returnValue;
            }

            Console.WriteLine($"Start recursive run for {operationName} from {rootDirectory}");

            Queue<string> directoriesToSearch = new Queue<string>();
            directoriesToSearch.Enqueue(rootDirectory);

            while (directoriesToSearch.Count > 0)
            {
                string currentSearchDirectory = directoriesToSearch.Dequeue();
                Console.WriteLine($"Collecting from {currentSearchDirectory}");

                // Adding subdirectories to search.
                foreach (string directory in Directory.EnumerateDirectories(currentSearchDirectory))
                {
                    directoriesToSearch.Enqueue(directory);
                }

                // Search files.
                foreach (string filePath in Directory.EnumerateFiles(currentSearchDirectory))
                {
                    fileOperation(filePath, returnValue);
                }
            }

            Console.WriteLine($"Finished recursive operation: {operationName}");
            return returnValue;
        }
    }
}