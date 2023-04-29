using BackupManager.App;
using BackupManager.App.Serialization;
using BackupManager.Domain.Configuration;
using BackupManager.Infra;
using BackupManager.Infra.Backup;
using BackupManager.Infra.Backup.Detectors;
using BackupManager.Infra.Backup.Services;
using BackupManager.Infra.Serialization;
using BackupManager.Infra.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using Serilog;
using Serilog.Core;
using Serilog.Events;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Dor Backup Service";
});

#pragma warning disable CA1416
LoggerProviderOptions.RegisterProviderOptions<EventLogSettings, EventLogLoggerProvider>(builder.Services);
#pragma warning restore CA1416

builder.Services.AddHostedService<WindowsBackgroundService>();
builder.Services.AddSingleton<IObjectSerializer, JsonSerializerWrapper>();
builder.Services.AddSingleton<BackupServiceFactory>();
builder.Services.AddSingleton<MediaDeviceBackupService>();
builder.Services.AddSingleton<DriveBackupService>();
builder.Services.AddSingleton<BackupOptionsDetector>();
builder.Services.AddSingleton<FilesHashesHandler>();
builder.Services.AddSingleton<IDuplicateChecker, DuplicateChecker>();
builder.Services.Configure<BackupServiceConfiguration>(builder.Configuration);
builder.Services.AddOptions();

builder.Configuration.AddJsonFile(Consts.SettingsFilePath, optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// See: https://github.com/dotnet/runtime/issues/47303
builder.Logging.AddConfiguration(
    builder.Configuration.GetSection("Logging"));

Logger? logger = new LoggerConfiguration()
    .WriteTo.File(Consts.LogsFilePath, LogEventLevel.Debug)
    .CreateLogger();

builder.Logging.AddSerilog(logger);

IHost host = builder.Build();
host.Run();