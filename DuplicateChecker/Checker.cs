using FileHashes;
using RecursiveDirectoryEnumaratorClass;
using System;
using System.Collections.Generic;
using System.IO;

namespace DuplicateChecker
{
    public class Checker
    {
        public void PrintDuplicateFiles(string rootDirectory, string duplicatesFilesTxtFile)
        {
            if (!Directory.Exists(Path.GetDirectoryName(duplicatesFilesTxtFile)))
            {
                Console.WriteLine($"{Path.GetDirectoryName(duplicatesFilesTxtFile)} does not exists");
                return;
            }

            PrintDuplicateFiles(GetDuplicateFiles(rootDirectory), duplicatesFilesTxtFile);
        }

        public FilesHashesHandler GetDuplicateFiles(string rootDirectory)
        {
            var directoryEnumarator = new RecursiveDirectoryEnumarator<FilesHashesHandler>();
            return directoryEnumarator.OperateRecursive(rootDirectory, AddFileHash, "Duplicate Files Search");
        }

        public void AddFileHash(string filePath, FilesHashesHandler filesHashesHandler)
        {
            filesHashesHandler.TryAddFileHash(filePath, addIfHashExist: true);
        }

        private void PrintDuplicateFiles(FilesHashesHandler filesHashesHandler, string outputPath)
        {
            using (StreamWriter writer = File.CreateText(outputPath))
            {
                foreach (KeyValuePair<string, List<string>> keyValuePair in filesHashesHandler)
                {
                    if (keyValuePair.Value.Count > 1)
                    {
                        writer.WriteLine($"Duplicate {keyValuePair.Key}");
                        keyValuePair.Value.ForEach(file => writer.WriteLine(file));
                        writer.WriteLine(string.Empty);
                    }
                }
            }
        }
    }
}