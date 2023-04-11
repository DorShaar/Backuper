using System;
using System.IO;

namespace BackupManager.Infra;

public static class Consts
{
    public const string HashesFileName = "hashes.txt";
    
    private const string mBackuperServiceDirectoryName = "BackuperService";
    
    private const string mSettingsDirectoryName = "Settings";
    private const string mSettingsFileName = "Setting.json";
    private const string mSettingsExampleFilePath = "SettingsExample.json";

    private const string mDataDirectoryName = "Data";
    private const string mDataFileName = "Data.json";
    private const string mBackupTimeDiaryFileName = "BackupTimeDiary";
    
    private static string mBackuperServiceDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        mBackuperServiceDirectoryName);

    private static string mSettingsDirectoryPath = Path.Combine(mBackuperServiceDirectoryPath, mSettingsDirectoryName);
    public static string SettingsFilePath = Path.Combine(mSettingsDirectoryPath, mSettingsFileName);
    public static string SettingsExampleFilePath = Path.Combine(mSettingsDirectoryPath, mSettingsExampleFilePath);
    
    public static string DataDirectoryPath = Path.Combine(mBackuperServiceDirectoryPath, mDataDirectoryName);
    // TOdO dOR use it
    public static string DataFilePath = Path.Combine(DataDirectoryPath, mDataFileName);
    public static string BackupTimeDiaryFilePath = Path.Combine(DataDirectoryPath, mBackupTimeDiaryFileName);
    
}