using Lyo.Exceptions;
using Lyo.IO.Temp.Models;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.IO.Temp;

/// <summary>Extension methods for registering IO temp service with dependency injection.</summary>
public static class Extensions
{
    /// <summary>Adds IO temp service with default options.</summary>
    public static IServiceCollection AddIOTempService(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        services.AddSingleton<IOTempServiceOptions>(_ => new());
        services.AddSingleton(CreateService);
        services.AddSingleton<IIOTempService>(provider => provider.GetRequiredService<IOTempService>());
        return services;
    }

    /// <summary>Adds IO temp service with options configuration callback.</summary>
    public static IServiceCollection AddIOTempService(this IServiceCollection services, Action<IOTempServiceOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        services.AddSingleton<IOTempServiceOptions>(_ => {
            var options = new IOTempServiceOptions();
            configure(options);
            return options;
        });

        services.AddSingleton(CreateService);
        services.AddSingleton<IIOTempService>(provider => provider.GetRequiredService<IOTempService>());
        return services;
    }

    /// <summary>Adds IO temp service with options bound from configuration.</summary>
    public static IServiceCollection AddIOTempServiceFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = IOTempServiceOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        services.AddSingleton<IOTempServiceOptions>(_ => {
            var options = new IOTempServiceOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return options;
        });

        services.AddSingleton(CreateService);
        services.AddSingleton<IIOTempService>(provider => provider.GetRequiredService<IOTempService>());
        return services;
    }

    private static IOTempService CreateService(IServiceProvider provider)
    {
        var logger = provider.GetService<ILogger<IOTempService>>();
        var loggerFactory = provider.GetService<ILoggerFactory>();
        var metrics = provider.GetService<IMetrics>();
        var options = provider.GetRequiredService<IOTempServiceOptions>();
        return new(options, logger, metrics, loggerFactory);
    }
}