using NDepend.Path;

namespace BackupManager.Infra.NDependExtensions
{
    public static class NDependExtensionMethods
    {
        public static string GetRelativePath(this IRelativeFilePath relativeFilePath)
        {
            string parentDirectoryPath = GetRelativeDirectoryPath(relativeFilePath.ParentDirectoryPath);

            if (string.IsNullOrEmpty(parentDirectoryPath))
                return relativeFilePath.FileName;

            return parentDirectoryPath + '/' + relativeFilePath.FileName;
        }

        private static string GetRelativeDirectoryPath(IRelativeDirectoryPath relativeDirectoryPath)
        {
            if (!relativeDirectoryPath.HasParentDirectory)
                return relativeDirectoryPath.DirectoryName;

            IRelativeDirectoryPath parentDirectory = relativeDirectoryPath.ParentDirectoryPath;

            string parentDirectoryPath = GetRelativeDirectoryPath(parentDirectory);

            if (string.IsNullOrEmpty(parentDirectoryPath))
                return relativeDirectoryPath.DirectoryName;

            return GetRelativeDirectoryPath(parentDirectory) + '/' + relativeDirectoryPath.DirectoryName;
        }
    }
}