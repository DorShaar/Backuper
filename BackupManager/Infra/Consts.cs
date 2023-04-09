using System;
using System.IO;
using NDepend.Path;

namespace BackupManager.Infra;

public static class Consts
{
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
    
    private static string mDataDirectoryPath = Path.Combine(mBackuperServiceDirectoryPath, mDataDirectoryName);
    public static string DataFilePath = Path.Combine(mDataDirectoryPath, mDataFileName);
    public static string BackupTimeDiaryFilePath = Path.Combine(mDataDirectoryPath, mBackupTimeDiaryFileName);
    
}