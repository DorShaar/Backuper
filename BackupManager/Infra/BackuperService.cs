using Backuper.Domain.Mapping;
using Backuper.App;
using System;
using System.Collections.Generic;
using System.IO;
using BackupManager.Infra.Hash;
using Backuper.Domain.Configuration;
using Microsoft.Extensions.Options;

namespace Backuper.Infra
{
    public class BackuperService : IBackuperService
    {
        private const string LastUpdateFileName = "LastUpdate.txt";
        private const string backupDirectoryName = "to_backup";

        private readonly FilesHashesHandler mFilesHashesHandler;
        private readonly IOptions<BackuperConfiguration> mConfiguration;
        private readonly string mBackupDriveDirectoryPath;

        public BackuperService(FilesHashesHandler filesHashesHandler,
            IOptions<BackuperConfiguration> configuration)
        {
            mFilesHashesHandler = filesHashesHandler ?? throw new ArgumentNullException(nameof(filesHashesHandler));
            mConfiguration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            mBackupDriveDirectoryPath = Path.Combine(Path.GetDirectoryName(mConfiguration.Value.DriveRootDirectory), backupDirectoryName);
        }

        public void BackupFiles()
        {
            DirectoriesMapping directoriesMapping = new DirectoriesMapping(mConfiguration.Value.DirectoriesCouples);

            foreach (DirectoriesMap directoriesMap in directoriesMapping)
            {
                Console.WriteLine($"Heading to directory {directoriesMap.SourceDirectory}");
                List<string> updatedFiles = FindUpdatedFiles(
                    Path.Combine(mBackupDriveDirectoryPath, directoriesMap.SourceDirectory),
                    GetLastUpdatedTime());

                if (updatedFiles.Count == 0)
                {
                    Console.WriteLine($"No updated files found in {directoriesMap.SourceDirectory}");
                    continue;
                }

                CopyUpdatedFiles(updatedFiles, directoriesMap, mFilesHashesHandler);
            }

            UpdateLastUpdatedTime();
        }

        private DateTime GetLastUpdatedTime()
        {
            string[] allLines = File.ReadAllLines(
                Path.Combine(mConfiguration.Value.DriveRootDirectory, LastUpdateFileName));
            string lastUpdateTime = allLines[^1];
            return DateTime.Parse(lastUpdateTime);
        }

        private void UpdateLastUpdatedTime()
        {
            File.AppendAllText(
                Path.Combine(mConfiguration.Value.DriveRootDirectory, LastUpdateFileName),
                Environment.NewLine + DateTime.Now.ToString());
        }

        private List<string> FindUpdatedFiles(string sourceDirectory, DateTime lastUpdateDateTime)
        {
            List<string> updatedFiles = new List<string>();

            if (!Directory.Exists(sourceDirectory))
            {
                Console.WriteLine($"{sourceDirectory} does not exists"); // Log error.
                return updatedFiles;
            }

            Console.WriteLine($"Start iterative operation for finding updated files from {sourceDirectory}");

            Queue<string> directoriesToSearch = new Queue<string>();
            directoriesToSearch.Enqueue(sourceDirectory);

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
                    AddUpdatedFileSince(filePath, updatedFiles, lastUpdateDateTime);
                }
            }

            Console.WriteLine($"Finished iterative operation for finding updated files from {sourceDirectory}");
            return updatedFiles;
        }

        private void AddUpdatedFileSince(string filePath, List<string> updatedFilesList, DateTime lastUpdateDateTime)
        {
            if (lastUpdateDateTime < (new FileInfo(filePath)).LastWriteTime)
                updatedFilesList.Add(filePath);
        }

        private void CopyUpdatedFiles(List<string> updatedFiles, DirectoriesMap directoriesMap, FilesHashesHandler filesHashesHandler)
        {
            Console.WriteLine($"Copying {updatedFiles.Count} updated files from {directoriesMap.SourceDirectory} to {directoriesMap.DestDirectory}");
            foreach (string updatedFile in updatedFiles)
            {
                string fileHash = HashCalculator.CalculateHash(updatedFile);
                if (filesHashesHandler.HashExists(fileHash))
                {
                    Console.WriteLine($"Duplication found - not performing copy: {updatedFile} with hash {fileHash}."); // log warning.
                    continue;
                }

                string outputFile = updatedFile.Replace(directoriesMap.SourceDirectory, directoriesMap.DestDirectory);
                outputFile = outputFile.Replace(mBackupDriveDirectoryPath, mConfiguration.Value.DriveRootDirectory);
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