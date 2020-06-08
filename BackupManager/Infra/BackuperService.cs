using Backuper.Domain.Mapping;
using Backuper.App;
using System;
using System.Collections.Generic;
using System.IO;
using Backuper.Infra;

namespace Backuper
{
    public class BackuperService : IBackuperService
    {
        public void BackupFiles(DirectoriesMapping directoriesMapping, DateTime lastUpdateDateTime, FilesHashesHandler filesHashesHandler)
        {
            foreach (DirectoriesMap directoriesMap in directoriesMapping)
            {
                Console.WriteLine($"Heading to directory {directoriesMap.SourceDirectory}");
                List<string> updatedFiles = FindUpdatedFiles(directoriesMap.SourceDirectory,
                    lastUpdateDateTime);

                if (updatedFiles.Count == 0)
                {
                    Console.WriteLine($"No updated files found in {directoriesMap.SourceDirectory}");
                    continue;
                }

                CopyUpdatedFiles(updatedFiles, directoriesMap, filesHashesHandler);
            }
        }

        private List<string> FindUpdatedFiles(string rootDirectory, DateTime lastUpdateDateTime)
        {
            List<string> updatedFiles = new List<string>();

            if (!Directory.Exists(rootDirectory))
            {
                Console.WriteLine($"{rootDirectory} does not exists"); // Log error.
                return updatedFiles;
            }

            Console.WriteLine($"Start recursive operation for finding updated files from {rootDirectory}");

            Queue<string> directoriesToSearch = new Queue<string>();
            directoriesToSearch.Enqueue(rootDirectory);

            while (directoriesToSearch.Count > 0)
            {
                string currentSearchDirectory = directoriesToSearch.Dequeue();
                Console.WriteLine($"Collecting from directory {currentSearchDirectory}");

                // Adding subdirectories to search.
                foreach (string directory in Directory.EnumerateDirectories(currentSearchDirectory))
                {
                    directoriesToSearch.Enqueue(directory);
                }

                // Search files.
                foreach (string filePath in Directory.EnumerateFiles(currentSearchDirectory))
                {
                    GetUpdatedFileSince(filePath, updatedFiles, lastUpdateDateTime);
                }
            }

            Console.WriteLine($"Finished recursive operation for finding updated files from {rootDirectory}");
            return updatedFiles;
        }

        private void GetUpdatedFileSince(string filePath, List<string> updatedFilesList, DateTime lastUpdateDateTime)
        {
            if (lastUpdateDateTime < (new FileInfo(filePath)).LastWriteTime)
                updatedFilesList.Add(filePath);
        }

        private void CopyUpdatedFiles(List<string> updatedFiles, DirectoriesMap directoriesMap, FilesHashesHandler filesHashesHandler)
        {
            Console.WriteLine($"Copying {updatedFiles.Count} updated files from {directoriesMap.SourceDirectory} to {directoriesMap.DestDirectory}");
            foreach (string updatedFile in updatedFiles)
            {
                string fileHash = FilesHashesHandler.GetFileHash(updatedFile);
                if (filesHashesHandler.HashExists(fileHash))
                {
                    Console.WriteLine($"Duplication found - not performing copy: {updatedFile} with hash {fileHash}."); // log warning.
                    continue;
                }

                string outputFile = updatedFile.Replace(directoriesMap.SourceDirectory, directoriesMap.DestDirectory);
                Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

                try
                {
                    File.Copy(updatedFile, outputFile, overwrite: true);
                    Console.WriteLine($"Copied {updatedFile} to {outputFile}");
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"Failed to copy {updatedFile} to {outputFile}. Exception: {ex.Message}"); // log error.
                }

                filesHashesHandler.AddFileHash(fileHash, outputFile);
                Console.WriteLine();
            }
        }
    }
}