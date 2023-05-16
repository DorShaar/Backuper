using System;
using BackupManager.App;
using BackupManager.App.Database;
using BackupManager.Domain.Configuration;
using BackupManager.Infra;
using BackupManager.Infra.Backup;
using BackupManager.Infra.Backup.Detectors;
using BackupManager.Infra.Backup.Services;
using BackupManager.Infra.DB.LocalJsonFileDatabase;
using BackupManager.Infra.DB.Mongo;
using BackupManager.Infra.DB.Mongo.Settings;
using BackupManager.Infra.DB.Sync;
using BackupManager.Infra.FileHashHandlers;
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
builder.Services.AddSingleton<BackupSettingsDetector>();
builder.Services.AddSingleton<IFilesHashesHandler, FilesHashesHandler>();
builder.Services.AddSingleton<IDuplicateChecker, DuplicateChecker>();

// Databases.
builder.Services.AddSingleton<MongoBackupServiceDatabase>();
builder.Services.AddSingleton<LocalJsonDatabase>();
builder.Services.AddSingleton<DatabasesSynchronizer>();
builder.Services.AddSingleton<IBackedUpFilesDatabase>(serviceProvider =>
{
    string? databaseType = serviceProvider.GetService<IConfiguration>()?["DatabaseType"];

    IBackedUpFilesDatabase? database = databaseType?.ToLower() switch
    {
        "mongo" => serviceProvider.GetService<MongoBackupServiceDatabase>(),
        "local" => serviceProvider.GetService<LocalJsonDatabase>(),
        _       => null
    };

    if (database is null)
    {
        throw new NullReferenceException($"{nameof(database)} is null. Please see DatabaseType section in appsettings.json file and verify it is one of: mongo, local");
    }

    return database;
});

builder.Services.Configure<BackupServiceConfiguration>(builder.Configuration);
builder.Services.Configure<MongoBackupServiceDatabaseSettings>(builder.Configuration.GetSection("BackupMongoDatabase"));
builder.Services.AddOptions();

builder.Configuration.AddJsonFile(Consts.SettingsFilePath, optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// TODO DOR add kibana
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