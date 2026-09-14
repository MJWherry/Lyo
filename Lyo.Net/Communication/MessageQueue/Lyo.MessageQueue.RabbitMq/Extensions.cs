using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;

namespace Lyo.MessageQueue.RabbitMq;

public static class Extensions
{
    private static IServiceCollection SetupRabbitMqServiceCore(IServiceCollection services, Dictionary<string, object?>? connectionProperties = null)
    {
        services.AddSingleton<IConnectionFactory>(provider => {
            var environment = provider.GetRequiredService<IHostEnvironment>();
            var options = provider.GetRequiredService<RabbitMqOptions>();
            var factory = new ConnectionFactory {
                HostName = options.Host,
                VirtualHost = options.VirtualHost,
                Port = options.Port,
                UserName = options.Username,
                Password = options.Password,
                ClientProvidedName = $"{Environment.MachineName} - {environment.ApplicationName} ({environment.EnvironmentName})",
                ClientProperties = connectionProperties ?? [],
                AutomaticRecoveryEnabled = options.AutomaticRecovery,
                TopologyRecoveryEnabled = options.AutomaticRecovery,
                NetworkRecoveryInterval = options.NetworkRecoveryInterval
            };

            return factory;
        });

        services.AddSingleton<RabbitMqService>()
            .AddSingleton<IRabbitMqService, RabbitMqService>(provider => provider.GetRequiredService<RabbitMqService>())
            .AddSingleton<IMqService, RabbitMqService>(provider => provider.GetRequiredService<RabbitMqService>());

        return services;
    }

    extension(IServiceCollection services)
    {
        /// <summary>Registers RabbitMQ using an options callback.</summary>
        public IServiceCollection SetupRabbitMqService(Dictionary<string, object?> connectionProperties, Action<RabbitMqOptions> configureOptions)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(connectionProperties);
            ArgumentHelpers.ThrowIfNull(configureOptions);

            // Bind and register options unless they are already present
            if (!services.Any(s => s.ServiceType == typeof(RabbitMqOptions))) {
                var options = new RabbitMqOptions();
                configureOptions(options);
                services.AddSingleton(options);
            }

            return SetupRabbitMqServiceCore(services, connectionProperties);
        }

        /// <summary>Registers RabbitMQ by binding options from IConfiguration.</summary>
        public IServiceCollection SetupRabbitMqServiceFromConfiguration(
            IConfiguration configuration,
            Dictionary<string, object?>? connectionProperties = null,
            string configSectionName = RabbitMqOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNull(connectionProperties);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);

            // Bind options from configuration unless they are already present
            if (!services.Any(s => s.ServiceType == typeof(RabbitMqOptions))) {
                var options = new RabbitMqOptions();
                configuration.GetSection(configSectionName).Bind(options);
                services.AddSingleton(options);
            }

            return SetupRabbitMqServiceCore(services, connectionProperties);
        }
    }
}