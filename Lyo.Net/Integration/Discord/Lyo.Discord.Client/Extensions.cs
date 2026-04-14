using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Discord.Client;

/// <summary>Dependency injection for <see cref="LyoDiscordClient" />.</summary>
public static class Extensions
{
    public static IServiceCollection AddDiscordClientFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = LyoDiscordClientOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        if (!services.Any(s => s.ServiceType == typeof(LyoDiscordClientOptions))) {
            services.AddSingleton(_ => {
                var options = new LyoDiscordClientOptions();
                var section = configuration.GetSection(configSectionName);
                if (section.Exists())
                    section.Bind(options);

                return options;
            });
        }

        services.AddSingleton<LyoDiscordClient>(provider => {
            var options = provider.GetRequiredService<LyoDiscordClientOptions>();
            var loggerFactory = provider.GetService<ILoggerFactory>();
            var httpClient = provider.GetService<HttpClient>();
            return new(options, loggerFactory?.CreateLogger<LyoDiscordClient>(), httpClient);
        });

        return services;
    }

    public static IServiceCollection AddDiscordClient(this IServiceCollection services, Action<LyoDiscordClientOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        services.AddSingleton(_ => {
            var options = new LyoDiscordClientOptions();
            configure(options);
            return options;
        });

        services.AddSingleton<LyoDiscordClient>(provider => {
            var options = provider.GetRequiredService<LyoDiscordClientOptions>();
            var loggerFactory = provider.GetService<ILoggerFactory>();
            var httpClient = provider.GetService<HttpClient>();
            return new(options, loggerFactory?.CreateLogger<LyoDiscordClient>(), httpClient);
        });

        return services;
    }

    public static IServiceCollection AddDiscordClient(this IServiceCollection services, LyoDiscordClientOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        services.AddSingleton(options);
        services.AddSingleton<LyoDiscordClient>(provider => {
            var loggerFactory = provider.GetService<ILoggerFactory>();
            var httpClient = provider.GetService<HttpClient>();
            return new(options, loggerFactory?.CreateLogger<LyoDiscordClient>(), httpClient);
        });

        return services;
    }
}