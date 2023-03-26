using Backuper.Domain.Configuration;
using BackupManager.Infra.Hash;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using NDepend.Path;
using BackupManager.Infra.NDependExtensions;

namespace BackupManager.Infra
{
    // TODO DOR understand what does it do
    public class UnregisteredHashesAdder
    {
        private readonly IOptions<BackuperConfiguration> mConfiguration;

        public UnregisteredHashesAdder(IOptions<BackuperConfiguration> configuration)
        {
            mConfiguration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public Dictionary<string, List<string>> UpdateUnregisteredFiles(Dictionary<string, List<string>> hashToFilePathDict)
        {
            HashSet<string> filePathsHashSet = GetAllFilePaths(hashToFilePathDict);
            List<string> unregisteredFiles = FindUnregisteredFiles(filePathsHashSet);
            return RegisterFiles(hashToFilePathDict, unregisteredFiles);
        }

        private HashSet<string> GetAllFilePaths(Dictionary<string, List<string>> hashToFilePathDict)
        {
            HashSet<string> filePathsHashSet = new HashSet<string>();

            foreach (List<string> paths in hashToFilePathDict.Values)
            {
                foreach (string path in paths)
                {
                    filePathsHashSet.Add(path);
                }
            }

            return filePathsHashSet;
        }

        private List<string> FindUnregisteredFiles(HashSet<string> filePathsHashSet)
        {
            if (!Directory.Exists(mConfiguration.Value.RootDirectory))
            {
                Console.WriteLine($"{mConfiguration.Value.RootDirectory} does not exists");
                return new List<string>();
            }

            return FindUnregisteredFilesIterative(filePathsHashSet);
        }

        private List<string> FindUnregisteredFilesIterative(HashSet<string> filePathsHashSet)
        {
            List<string> unregisteredFiles = new List<string>();

            Console.WriteLine($"Start iterative operation for finding unregistered files from {mConfiguration.Value.RootDirectory}");

            Queue<string> directoriesToSearch = new Queue<string>();
            directoriesToSearch.Enqueue(mConfiguration.Value.RootDirectory);

            mConfiguration.Value.RootDirectory.TryGetAbsoluteDirectoryPath(out IAbsoluteDirectoryPath absoluteDirectoryPath);

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
                    filePath.TryGetAbsoluteFilePath(out IAbsoluteFilePath absoluteFilePath);
                    IRelativeFilePath relativeFilePath = absoluteFilePath.GetRelativePathFrom(absoluteDirectoryPath);

                    string relativePath = relativeFilePath.GetRelativePath();
                    if (!filePathsHashSet.Contains(relativePath))
                        unregisteredFiles.Add(relativePath);
                }
            }

            Console.WriteLine($"Finished iterative operation for finding unregistered files from {mConfiguration.Value.RootDirectory}");
            return unregisteredFiles;
        }

        private Dictionary<string, List<string>> RegisterFiles(Dictionary<string, List<string>> hashToFilePathDict,
            List<string> unregisteredFiles)
        {
            foreach(string unregisteredFile in unregisteredFiles)
            {
                string unregisteredFileFullPath = mConfiguration.Value.RootDirectory + '/' + unregisteredFile;

                string fileHash = HashCalculator.CalculateHash(unregisteredFileFullPath);
                if (hashToFilePathDict.TryGetValue(fileHash, out List<string> filePaths))
                {
                    filePaths.Add(unregisteredFile);
                }
                else
                {
                    hashToFilePathDict[fileHash] = new List<string> { unregisteredFile };
                }
            }

            return hashToFilePathDict;
        }
    }
}