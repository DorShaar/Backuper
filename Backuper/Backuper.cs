using FileHashes;
using RecursiveDirectoryEnumaratorClass;
using System;
using System.Collections.Generic;
using System.IO;

namespace BackuperApp
{
    public class Backuper
    {
        private DateTime mLastUpdateDateTime;

        public void BackupFiles(DirectoriesBinding directoriesBinding, DateTime lastUpdateDateTime, FilesHashesHandler filesHashesHandler)
        {
            mLastUpdateDateTime = lastUpdateDateTime;

            var directoryEnumarator = new RecursiveDirectoryEnumarator<List<string>>();

            foreach (DirectoriesCouple directoriesCouple in directoriesBinding)
            {
                Console.WriteLine($"Backuping from {directoriesCouple.SourceDirectory}");
                List<string> updatedFiles = directoryEnumarator.OperateRecursive(
                    directoriesCouple.SourceDirectory,
                    GetUpdatedFileSince,
                    "Get Updated Files");

                if (updatedFiles.Count == 0)
                {
                    Console.WriteLine("No updated files found");
                    continue;
                }

                Console.WriteLine($"Copying {updatedFiles.Count} updated files from {directoriesCouple.SourceDirectory} to {directoriesCouple.DestDirectory}");
                foreach (string updatedFile in updatedFiles)
                {
                    string fileHash = filesHashesHandler.GetFileHash(updatedFile);
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

        private void GetUpdatedFileSince(string filePath, List<string> updatedFilesList)
        {
            if (mLastUpdateDateTime < (new FileInfo(filePath)).LastWriteTime)
                updatedFilesList.Add(filePath);
        }
    }
}