using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BackupManager.App.Backup;
using BackupManager.App.Backup.Detectors;
using BackupManager.App.Backup.Services;
using BackupManager.App.Database.Sync;
using BackupManager.Domain.Configuration;
using BackupManager.Domain.Settings;
using BackupManager.Infra.Backup;
using BackupManager.Infra.Backup.Detectors;
using BackupManager.Infra.Service;
using BackupManagerCore;
using BackupManagerCore.Mapping;
using BackupManagerCore.Settings;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Temporaries;
using Xunit;

namespace BackupManagerTests.Infra.Service;

[Collection("BackupServiceTests Usage")]
public class WindowsBackgroundServiceTests : TestsBase
{
	[Fact]
	public async Task StartAsync_SettingsFilesNotExists_ThrowsFileNotFoundException()
	{
		BackupServiceFactory backupServiceFactory = A.Dummy<BackupServiceFactory>();
		BackupSettingsDetector backupSettingsDetector = A.Dummy<BackupSettingsDetector>();
		IDatabasesSynchronizer databasesSynchronizer = A.Dummy<IDatabasesSynchronizer>();
		IOptionsMonitor<BackupServiceConfiguration> backupServiceOptions = A.Dummy<IOptionsMonitor<BackupServiceConfiguration>>();

		WindowsBackgroundService windowsBackgroundService = new(backupServiceFactory,
																backupSettingsDetector,
																databasesSynchronizer,
																backupServiceOptions,
																NullLogger<WindowsBackgroundService>.Instance);

		_ = Directory.CreateDirectory(Path.GetDirectoryName(Consts.SettingsFilePath) ?? throw new NullReferenceException());
		
		await Assert.ThrowsAsync<FileNotFoundException>(async () => await windowsBackgroundService.StartAsync(CancellationToken.None).ConfigureAwait(false));
	}
	
	[Fact]
	public async Task StartAsync_ShouldBackupToKnownDirectoryIsFalse_TokenNotRecognized_NoBackupPerformed()
	{
		IBackupServiceFactory backupServiceFactory = A.Fake<IBackupServiceFactory>();

		IBackupSettingsDetector backupSettingsDetector = A.Fake<IBackupSettingsDetector>();
		BackupSerializedSettings backupSerializedSettings = new()
		{
            IsFromInstallation = true,
            DirectoriesSourcesToDirectoriesDestinationMap = new List<DirectoriesMap>(),
			ShouldBackupToKnownDirectory = false,
			Token = Guid.NewGuid().ToString()
		};
		BackupSettings backupSettings = new(backupSerializedSettings, string.Empty);
		CancellationTokenSource cancellationTokenSource = new();
		A.CallTo(() => backupSettingsDetector.DetectBackupSettings(A<CancellationToken>.Ignored))
		 .Returns(new List<BackupSettings>
		 {
			 backupSettings
		 })
		 .Once()
		 .Then
		 .Invokes(() => cancellationTokenSource.Cancel());
		
		IDatabasesSynchronizer databasesSynchronizer = A.Dummy<IDatabasesSynchronizer>();
		IOptionsMonitor<BackupServiceConfiguration> backupServiceOptions = A.Dummy<IOptionsMonitor<BackupServiceConfiguration>>();
		backupServiceOptions.CurrentValue.CheckForBackupSettingsInterval = TimeSpan.FromMilliseconds(20);

		WindowsBackgroundService windowsBackgroundService = new(backupServiceFactory,
																backupSettingsDetector,
																databasesSynchronizer,
																backupServiceOptions,
																NullLogger<WindowsBackgroundService>.Instance);

		_ = Directory.CreateDirectory(Path.GetDirectoryName(Consts.SettingsFilePath) ?? throw new NullReferenceException());

		using TempFile settingsFilePath = new(Consts.SettingsFilePath);
		await File.WriteAllTextAsync(settingsFilePath.Path, "a", CancellationToken.None);
		
		await windowsBackgroundService.StartAsync(cancellationTokenSource.Token);
		
		A.CallTo(() => backupServiceFactory.Create(A<BackupSettings>.Ignored)).MustNotHaveHappened();
	}

	[Fact]
	public async Task ExecuteAsync_ShouldBackupToKnownDirectoryIsFalse_TokenIsRecognized_BackupPerformed()
	{
		IBackupService backupService = A.Fake<IBackupService>();
		IBackupServiceFactory backupServiceFactory = A.Fake<IBackupServiceFactory>();
		A.CallTo(() => backupServiceFactory.Create(A<BackupSettings>.Ignored)).Returns(backupService);

		IBackupSettingsDetector backupSettingsDetector = A.Fake<IBackupSettingsDetector>();
		BackupSerializedSettings backupSerializedSettings = new()
		{
			IsFromInstallation = true,
			DirectoriesSourcesToDirectoriesDestinationMap = new List<DirectoriesMap>(),
			ShouldBackupToKnownDirectory = false,
			Token = Guid.NewGuid().ToString()
		};
		BackupSettings backupSettings = new(backupSerializedSettings, string.Empty);
		CancellationTokenSource cancellationTokenSource = new();
		A.CallTo(() => backupSettingsDetector.DetectBackupSettings(A<CancellationToken>.Ignored))
		 .Returns(new List<BackupSettings>
		 {
			 backupSettings
		 })
		 .Once()
		 .Then
		 .Invokes(() => cancellationTokenSource.Cancel());

		_ = Directory.CreateDirectory(Path.GetDirectoryName(Consts.KnownTokensFilePath) ?? throw new NullReferenceException());
		await File.WriteAllTextAsync(Consts.KnownTokensFilePath, backupSerializedSettings.Token, CancellationToken.None);
		
		IDatabasesSynchronizer databasesSynchronizer = A.Dummy<IDatabasesSynchronizer>();
		IOptionsMonitor<BackupServiceConfiguration> backupServiceOptions = A.Dummy<IOptionsMonitor<BackupServiceConfiguration>>();
		backupServiceOptions.CurrentValue.CheckForBackupSettingsInterval = TimeSpan.FromMilliseconds(20);

		WindowsBackgroundService windowsBackgroundService = new(backupServiceFactory,
																backupSettingsDetector,
																databasesSynchronizer,
																backupServiceOptions,
																NullLogger<WindowsBackgroundService>.Instance);

		_ = Directory.CreateDirectory(Path.GetDirectoryName(Consts.SettingsFilePath) ?? throw new NullReferenceException());

		using TempFile settingsFilePath = new(Consts.SettingsFilePath);
		await File.WriteAllTextAsync(settingsFilePath.Path, "a", CancellationToken.None);
		
		await windowsBackgroundService.StartAsync(cancellationTokenSource.Token);

		// Must wait for few seconds to allow the ExecuteAsync begin.
		await Task.Delay(3000, CancellationToken.None);

		A.CallTo(() => backupService.BackupFiles(A<BackupSettings>.Ignored, A<CancellationToken>.Ignored))
		 .MustHaveHappenedOnceExactly();
	}
}