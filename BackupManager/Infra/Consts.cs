using System;
using System.IO;

namespace BackupManager.Infra;

public static class Consts
{
    #region Top Directory
    public const string BackupServiceDirectoryName = "BackupService";
    
    // Used for tests only.
    private const string mBackupServiceTestsDirectoryName = "BackupServiceTests";
    
    private static string mBackupServiceDirectoryPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        mBackupServiceTestsDirectoryName); // For Tests.
        // BackupServiceDirectoryName); // For Production.
    #endregion Top Directory

    #region Settings
    private const string mSettingsDirectoryName = "Settings";
    public const string SettingsFileName = "BackupSetting.json";
    private const string mSettingsExampleFilePath = "SettingsExample.json";
    
    private static string mSettingsDirectoryPath => Path.Combine(mBackupServiceDirectoryPath, mSettingsDirectoryName);
    public static string SettingsFilePath => Path.Combine(mSettingsDirectoryPath, SettingsFileName);
    public static string SettingsExampleFilePath => Path.Combine(mSettingsDirectoryPath, mSettingsExampleFilePath);
    #endregion Settings

    #region Logs
    private const string mLogsDirectoryName = "Logs";
    private const string mLogsFilesName = "backuper.log";
    
    private static string mLogsDirectoryPath => Path.Combine(mBackupServiceDirectoryPath, mLogsDirectoryName);
    public static string LogsFilePath => Path.Combine(mLogsDirectoryPath, mLogsFilesName);
    #endregion Logs
    
    #region Data
    private const string mDataDirectoryName = "Data";
    private const string mBackupsDirectoryName = "Backups";
    private const string mDataFileName = "Data.json";
    private const string mBackupTimeDiaryFileName = "BackupTimeDiary";

    public static string DataDirectoryPath => Path.Combine(mBackupServiceDirectoryPath, mDataDirectoryName);
    public static string DataFilePath => Path.Combine(DataDirectoryPath, mDataFileName);
    public static string BackupTimeDiaryFilePath => Path.Combine(DataDirectoryPath, mBackupTimeDiaryFileName);
    public static string BackupsDirectoryPath => Path.Combine(DataDirectoryPath, mBackupsDirectoryName);
    #endregion Data

    #region Temp

    private const string mTempDirectoryName = "Temp";
    public static string TempDirectoryPath => Path.Combine(mBackupServiceDirectoryPath, mTempDirectoryName);
    #endregion Temp
}