using RecursiveDirectoryEnumaratorClass;
using System;
using System.Collections.Generic;
using System.IO;

namespace BackuperApp
{
    public class Backuper
    {
        private DateTime mLastUpdateDateTime;

        public List<string> BackupFiles(
            DirectoriesBinding directoriesBinding, 
            DateTime lastUpdateDateTime, 
            Dictionary<string, List<string>> hashToFilePath)
        {
            if (hashToFilePath == null || hashToFilePath.Count == 0)
                throw new InvalidOperationException("Please run Duplicate Check or configure duplicates file");

            List<string> totalUpdatedFiles = new List<string>();

            mLastUpdateDateTime = lastUpdateDateTime;

            var directoryEnumarator = new RecursiveDirectoryEnumarator<List<string>>();

            foreach (DirectoriesCouple directoriesCouple in directoriesBinding)
            {
                Console.WriteLine($"Backuping from {directoriesCouple.SourceDirectory}");
                List<string> updatedFiles = directoryEnumarator.OperateRecursive(
                    directoriesCouple.SourceDirectory, 
                    GetUpdatedFileSince,
                    "Get Updated Files");

                totalUpdatedFiles.AddRange(updatedFiles);

                Console.WriteLine($"Copying updated files from {directoriesCouple.SourceDirectory} to {directoriesCouple.DestDirectory}");
                foreach(string updatedFile in updatedFiles)
                {
                    string outputFile = updatedFile.Replace(
                        directoriesCouple.SourceDirectory, directoriesCouple.DestDirectory);
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

            return totalUpdatedFiles;
        }

        private void GetUpdatedFileSince(string filePath, List<string> updatedFilesList)
        {
            if (mLastUpdateDateTime < (new FileInfo(filePath)).LastWriteTime)
                updatedFilesList.Add(filePath);
        }
    }
}