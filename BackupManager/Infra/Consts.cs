using System;
using System.IO;

namespace BackupManager.Infra;

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
    private static string mBackupServiceDirectoryPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        // mBackupServiceTestsDirectoryName); // For Tests.
        BackupServiceDirectoryName); // For Production.
    #endregion Top Directory

    #region Settings
    private const string mSettingsDirectoryName = "Settings";
    public const string SettingsFileName = "BackupSetting.json";
    private const string mSettingsExampleFilePath = "SettingsExample.json";
    private const string mKnownTokensFileName = "KnownTokens";
    
    private static string mSettingsDirectoryPath => Path.Combine(mBackupServiceDirectoryPath, mSettingsDirectoryName);
    public static string SettingsFilePath => Path.Combine(mSettingsDirectoryPath, SettingsFileName);
    public static string SettingsExampleFilePath => Path.Combine(mSettingsDirectoryPath, mSettingsExampleFilePath);
    public static string KnownTokensFilePath => Path.Combine(mSettingsDirectoryPath, mKnownTokensFileName);
    #endregion Settings

    #region Logs
    private const string mLogsDirectoryName = "Logs";
    private const string mLogsFilesNameWithoutExtension = "backuper";
    
    private static string mLogsDirectoryPath => Path.Combine(mBackupServiceDirectoryPath, mLogsDirectoryName);
    public static string LogsFilePathWithoutExtension => Path.Combine(mLogsDirectoryPath, mLogsFilesNameWithoutExtension);
    #endregion Logs
    
    #region Data
    private const string mDataDirectoryName = "Data";
    private const string mBackupsDirectoryName = "Backups";
    private const string mWaitingApprovalDirectoryName = "WaitingApproval";
    private const string mReadyToBackupDirectoryName = "ReadyToBackup";
    private const string mBackedUpDirectoryName = "BackedUp";
    private const string mBackupTimeDiaryFileName = "BackupTimeDiary";
    public const string LocalDatabaseExtension = "json";

    public static string DataDirectoryPath => Path.Combine(mBackupServiceDirectoryPath, mDataDirectoryName);
    public static string BackupTimeDiaryFilePath => Path.Combine(DataDirectoryPath, mBackupTimeDiaryFileName);
    private static string mBackupsDirectoryPath => Path.Combine(DataDirectoryPath, mBackupsDirectoryName);
    public static string WaitingApprovalDirectoryPath => Path.Combine(mBackupsDirectoryPath, mWaitingApprovalDirectoryName);
    public static string ReadyToBackupDirectoryPath => Path.Combine(mBackupsDirectoryPath, mReadyToBackupDirectoryName);
    public static string BackedUpDirectoryPath => Path.Combine(mBackupsDirectoryPath, mBackedUpDirectoryName);
    #endregion Data
    
    #region Database
    public const string BackupFilesCollectionName = "FilesBackup";
    
    /// <summary>
    /// {0} - known-id.
    /// </summary>
    public static string BackupFilesForKnownDriveCollectionTemplate => "Data-{0}";
    #endregion Database

    #region Temp

    private const string mTempDirectoryName = "Temp";
    public static string TempDirectoryPath => Path.Combine(mBackupServiceDirectoryPath, mTempDirectoryName);
    #endregion Temp
}