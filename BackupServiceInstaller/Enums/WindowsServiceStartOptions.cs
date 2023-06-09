﻿namespace BackupServiceInstaller.Enums;

public enum WindowsServiceStartOptions
{
	Start = 0x00000000,
	SystemStart = 0x00000001,
	AutoStart = 0x00000002,
	DemandStart = 0x00000003,
	Disabled = 0x00000004
}