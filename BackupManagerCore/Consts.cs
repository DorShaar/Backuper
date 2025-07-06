namespace BackupManagerCore;

public static class Consts
{   
    #region Application Configuration
    public const string DatabasesTypesSection = "DatabasesTypes";

    public static string[] AllowedDatabasesTypes =
    {
        "mongo",
        "local"
    };
    #endregion Application Configuration
    
    #region Top Directory
    public const string BackupServiceDirectoryName = "BackupService";
    
    // Used for tests only.
    private const string mBackupServiceTestsDirectoryName = "BackupServiceTests";
    
    // TODO DOR now
    public static string BackupServiceDirectoryPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        // mBackupServiceTestsDirectoryName); // For Tests.
        BackupServiceDirectoryName); // For Production.
    #endregion Top Directory
    
    #region Settings
    private const string mSettingsDirectoryName = "Settings";
    public const string SettingsFileName = "BackupSetting.json";
    private const string mSettingsExampleFilePath = "SettingsExample.json";
    private const string mKnownTokensFileName = "KnownTokens";
    
    public static string SettingsDirectoryPath => Path.Combine(BackupServiceDirectoryPath, mSettingsDirectoryName);
    public static string SettingsFilePath => Path.Combine(SettingsDirectoryPath, SettingsFileName);
    public static string SettingsExampleFilePath => Path.Combine(SettingsDirectoryPath, mSettingsExampleFilePath);
    public static string KnownTokensFilePath => Path.Combine(SettingsDirectoryPath, mKnownTokensFileName);
    #endregion Settings

    #region Logs
    private const string mLogsDirectoryName = "Logs";
    private const string mLogsFilesNameWithoutExtension = "backuper";
    
    public static string LogsDirectoryPath => Path.Combine(BackupServiceDirectoryPath, mLogsDirectoryName);
    public static string LogsFilePathWithoutExtension => Path.Combine(LogsDirectoryPath, mLogsFilesNameWithoutExtension);
    #endregion Logs
    
    public struct Data
    {
        private const string mDataDirectoryName = "Data";
        private const string mBackupsDirectoryName = "Backups";
        private const string mWaitingApprovalDirectoryName = "WaitingApproval";
        private const string mReadyToBackupDirectoryName = "ReadyToBackup";
        private const string mBackedUpDirectoryName = "BackedUp";
        private const string mBackupTimeDiaryFileName = "BackupTimeDiary";
        public const string LocalDatabaseExtension = "json";

        public static string DataDirectoryPath => Path.Combine(BackupServiceDirectoryPath, mDataDirectoryName);
        public static string BackupTimeDiaryFilePath => Path.Combine(DataDirectoryPath, mBackupTimeDiaryFileName);
        public static string BackupsDirectoryPath => Path.Combine(DataDirectoryPath, mBackupsDirectoryName);
        public static string WaitingApprovalDirectoryPath => Path.Combine(BackupsDirectoryPath, mWaitingApprovalDirectoryName);
        public static string ReadyToBackupDirectoryPath => Path.Combine(BackupsDirectoryPath, mReadyToBackupDirectoryName);
        public static string BackedUpDirectoryPath => Path.Combine(BackupsDirectoryPath, mBackedUpDirectoryName);
    }
    
    public struct Database
    {
        public const string BackupFilesCollectionName = "FilesBackup";
    
        /// <summary>
        /// {0} - known-id.
        /// </summary>
        public static string BackupFilesForKnownDriveCollectionTemplate => "Data-{0}";
    }

    public struct ServiceAndCLI
    {
        public const string ServiceName = "Dor Backuper Service";
        private const string ServiceDirectoryName = "bin";
        private const string BackupServiceCliName = "backup.exe";
        private const string ServiceFileName = "BackupManager.exe";

        public static string ServiceDirectoryPath => Path.Combine(BackupServiceDirectoryPath, ServiceDirectoryName);
        public static string CliFilePath => Path.Combine(ServiceDirectoryPath, BackupServiceCliName);
        public static string ServicePath => Path.Combine(ServiceDirectoryPath, ServiceFileName);
    }

    #region Temp

    private const string mTempDirectoryName = "Temp";
    public static string TempDirectoryPath => Path.Combine(BackupServiceDirectoryPath, mTempDirectoryName);
    #endregion Temp
}