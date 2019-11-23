using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serializer;
using Serializer.Interface;
using System;

namespace BackupManager
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

            RegisterConfiguration(serviceCollection);

            return serviceCollection.BuildServiceProvider();
        }

        private void RegisterConfiguration(ServiceCollection serviceCollection)
        {
            IConfigurationBuilder configurationBuilder = 
                new ConfigurationBuilder()
                .AddJsonFile(@"config\BackuperConfig.json", optional: false); // Adds json configuration.

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