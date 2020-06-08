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
        public void BackupFiles(DirectoriesMapping directoriesBinding, DateTime lastUpdateDateTime, FilesHashesHandler filesHashesHandler)
        {
            foreach (DirectoriesMap directoriesCouple in directoriesBinding)
            {
                Console.WriteLine($"Backuping from {directoriesCouple.SourceDirectory}");
                List<string> updatedFiles = FindUpdatedFiles(directoriesCouple.SourceDirectory,
                    lastUpdateDateTime);

                if (updatedFiles.Count == 0)
                {
                    Console.WriteLine("No updated files found");
                    continue;
                }

                Console.WriteLine($"Copying {updatedFiles.Count} updated files from {directoriesCouple.SourceDirectory} to {directoriesCouple.DestDirectory}");
                foreach (string updatedFile in updatedFiles)
                {
                    string fileHash = FilesHashesHandler.GetFileHash(updatedFile);
                    if (filesHashesHandler.HashExists(fileHash))
                    {
                        Console.WriteLine($"DUP: {updatedFile} with hash {fileHash}");
                        continue;
                    }

                    string outputFile = updatedFile.Replace(directoriesCouple.SourceDirectory, directoriesCouple.DestDirectory);
                    Console.WriteLine($"COPY: {updatedFile} to {outputFile}");
                    Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

                    try
                    {
                        File.Copy(updatedFile, outputFile);
                    }
                    catch (IOException)
                    {
                        Console.WriteLine($"Failed to copy {updatedFile} to {outputFile}. Check if {outputFile} is already exists");
                    }

                    filesHashesHandler.AddFileHash(fileHash, outputFile);
                    Console.WriteLine();
                }
            }
        }

        private List<string> FindUpdatedFiles(string rootDirectory, DateTime lastUpdateDateTime)
        {
            List<string> updatedFiles = new List<string>();

            if (!Directory.Exists(rootDirectory))
            {
                Console.WriteLine($"{rootDirectory} does not exists");
                return updatedFiles;
            }

            Console.WriteLine($"Start recursive operation for finding updated files from {rootDirectory}");

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
    }
}