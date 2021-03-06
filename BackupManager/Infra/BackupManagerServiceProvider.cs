﻿using Backuper.App.Serialization;
using Backuper.Domain.Configuration;
using Backuper.Infra.Serialization;
using Backuper.App;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using BackupManager.Infra;

namespace Backuper.Infra
{
    internal class BackupManagerServiceProvider : IServiceProvider
    {
        private readonly IServiceProvider mServiceProvider;

        public BackupManagerServiceProvider()
        {
            mServiceProvider = CreateServiceProvider();
        }

        private IServiceProvider CreateServiceProvider()
        {
            ServiceCollection serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton<IObjectSerializer, JsonSerializerWrapper>();
            serviceCollection.AddSingleton<IBackuperService, BackuperService>();
            serviceCollection.AddSingleton<IDuplicateChecker, DuplicateChecker>();
            serviceCollection.AddSingleton<UnregisteredHashesAdder>();
            serviceCollection.AddSingleton<FilesHashesHandler>();

            RegisterConfiguration(serviceCollection);

            return serviceCollection.BuildServiceProvider();
        }

        private void RegisterConfiguration(ServiceCollection serviceCollection)
        {
            IConfigurationBuilder configurationBuilder =
                new ConfigurationBuilder()
                .AddJsonFile("Domain/Configuration/BackuperConfig.json", optional: false); // Adds json configuration.

            IConfiguration configuration = configurationBuilder.Build();

            // Binds between IConfiguration to BackuperConfiguration.
            serviceCollection.Configure<BackuperConfiguration>(configuration);
            serviceCollection.AddOptions();
        }

        public object GetService(Type type)
        {
            return mServiceProvider.GetRequiredService(type);
        }
    }
}