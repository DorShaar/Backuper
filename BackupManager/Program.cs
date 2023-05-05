using BackupManager.App;
using BackupManager.Domain.Configuration;
using BackupManager.Domain.Hash;
using BackupManager.Infra;
using BackupManager.Infra.Backup;
using BackupManager.Infra.Backup.Detectors;
using BackupManager.Infra.Backup.Services;
using BackupManager.Infra.Service;
using JsonSerialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using Serilog;
using Serilog.Core;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Dor Backup Service";
});

#pragma warning disable CA1416
LoggerProviderOptions.RegisterProviderOptions<EventLogSettings, EventLogLoggerProvider>(builder.Services);
#pragma warning restore CA1416

builder.Services.AddHostedService<WindowsBackgroundService>();
builder.Services.AddSingleton<IJsonSerializer, JsonSerializer>();
builder.Services.AddSingleton<BackupServiceFactory>();
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
    .MinimumLevel.Verbose()
    .WriteTo.RollingFile(Consts.LogsFilePathWithoutExtension+"-{Date}.log")
    .CreateLogger();

builder.Logging.AddSerilog(logger);

IHost host = builder.Build();
host.Run();