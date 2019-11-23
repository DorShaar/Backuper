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

                Console.WriteLine($"Copying updated files from {directoriesCouple.SourceDirectory} to {directoriesCouple.DestDirectory}");
                foreach(string updatedFile in updatedFiles)
                {
                    if (!filesHashesHandler.TryAddFileHash(updatedFile))
                        continue;

                    string outputFile = updatedFile.Replace(directoriesCouple.SourceDirectory, directoriesCouple.DestDirectory);
                    Console.WriteLine($"{updatedFile} will be copied to {outputFile}");
                    Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

                    try
                    {
                        File.Copy(updatedFile, outputFile);
                    }
                    catch (IOException)
                    {
                        Console.WriteLine($"Failed to copy {updatedFile} to {outputFile}. Check if {outputFile} is already exists");
                    }
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