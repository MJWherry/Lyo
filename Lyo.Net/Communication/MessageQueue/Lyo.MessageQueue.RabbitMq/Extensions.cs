using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;

namespace Lyo.MessageQueue.RabbitMq;

public static class Extensions
{
    /// <summary>Sets up RabbitMQ service with explicit options configuration via action.</summary>
    public static IServiceCollection SetupRabbitMqService(
        this IServiceCollection services,
        Dictionary<string, object?> connectionProperties,
        Action<RabbitMqOptions> configureOptions)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(connectionProperties, nameof(connectionProperties));
        ArgumentHelpers.ThrowIfNull(configureOptions, nameof(configureOptions));

        // Configure and register options (only if not already registered)
        if (!services.Any(s => s.ServiceType == typeof(RabbitMqOptions))) {
            var options = new RabbitMqOptions();
            configureOptions(options);
            services.AddSingleton(options);
        }

        return SetupRabbitMqServiceCore(services, connectionProperties);
    }

    /// <summary>Sets up RabbitMQ service using configuration binding from IConfiguration.</summary>
    public static IServiceCollection SetupRabbitMqServiceFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        Dictionary<string, object?>? connectionProperties = null,
        string configSectionName = RabbitMqOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNull(connectionProperties, nameof(connectionProperties));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));

        // Register options using configuration binding (only if not already registered)
        if (!services.Any(s => s.ServiceType == typeof(RabbitMqOptions))) {
            services.AddSingleton<RabbitMqOptions>(_ => {
                var section = configuration.GetSection(configSectionName);
                var options = new RabbitMqOptions();
                section.Bind(options);
                return options;
            });
        }

        return SetupRabbitMqServiceCore(services, connectionProperties);
    }

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
                ClientProperties = connectionProperties ?? []
            };

            return factory;
        });

        services.AddSingleton<RabbitMqService>()
            .AddSingleton<IRabbitMqService, RabbitMqService>(provider => provider.GetRequiredService<RabbitMqService>())
            .AddSingleton<IMqService, RabbitMqService>(provider => provider.GetRequiredService<RabbitMqService>());

        return services;
    }
}