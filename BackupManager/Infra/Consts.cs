using System;
using System.IO;

namespace BackupManager.Infra;

public static class Consts
{
    #region Top Directory
    private const string mBackuperServiceDirectoryName = "BackuperService";
    
    // Used for tests only.
    private const string mBackupServiceTestsDirectoryName = "BackuperServiceTests";
    
    private static string mBackupServiceDirectoryPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        // mBackupServiceTestsDirectoryName); // For Tests.
        mBackuperServiceDirectoryName); // For Production.
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
    private const string mDataFileName = "Data.json";
    private const string mBackupTimeDiaryFileName = "BackupTimeDiary";

    public static string DataDirectoryPath => Path.Combine(mBackupServiceDirectoryPath, mDataDirectoryName);
    public static string DataFilePath => Path.Combine(DataDirectoryPath, mDataFileName);
    public static string BackupTimeDiaryFilePath => Path.Combine(DataDirectoryPath, mBackupTimeDiaryFileName);
    #endregion Data
}