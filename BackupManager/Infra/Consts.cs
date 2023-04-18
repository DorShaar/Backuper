using System;
using System.IO;

namespace BackupManager.Infra;

public static class Consts
{
    private const string mBackuperServiceDirectoryName = "BackuperService";
    
    // Used for tests only.
    private const string mBackuperServiceTestsDirectoryName = "BackuperServiceTests";
    
    private const string mSettingsDirectoryName = "Settings";
    public const string SettingsFileName = "BackupSetting.json";
    private const string mSettingsExampleFilePath = "SettingsExample.json";

    private const string mDataDirectoryName = "Data";
    private const string mDataFileName = "Data.json";
    private const string mBackupTimeDiaryFileName = "BackupTimeDiary";

    public static string BackuperServiceDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        mBackuperServiceTestsDirectoryName); // For Tests.
        // mBackuperServiceDirectoryName); // For Production.

    private static string mSettingsDirectoryPath = Path.Combine(BackuperServiceDirectoryPath, mSettingsDirectoryName);
    public static string SettingsFilePath = Path.Combine(mSettingsDirectoryPath, SettingsFileName);
    public static string SettingsExampleFilePath = Path.Combine(mSettingsDirectoryPath, mSettingsExampleFilePath);
    
    public static string DataDirectoryPath = Path.Combine(BackuperServiceDirectoryPath, mDataDirectoryName);
    // TOdO dOR use it
    public static string DataFilePath = Path.Combine(DataDirectoryPath, mDataFileName);
    public static string BackupTimeDiaryFilePath = Path.Combine(DataDirectoryPath, mBackupTimeDiaryFileName);
    
}