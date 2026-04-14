using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.FileMetadataStore;

public static class Extensions
{
    /// <summary>Adds local file metadata store to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="rootDirectoryPath">The root directory path for storing metadata</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddLocalFileMetadataStore(this IServiceCollection services, string rootDirectoryPath)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(rootDirectoryPath, nameof(rootDirectoryPath));
        services.AddSingleton<LocalFileMetadataStore>(provider => {
            var loggerFactory = provider.GetService<ILoggerFactory>();
            return new(rootDirectoryPath, loggerFactory);
        });

        services.AddSingleton<IFileMetadataStore>(provider => provider.GetRequiredService<LocalFileMetadataStore>());
        return services;
    }

    /// <summary>Adds local file metadata store to the service collection using a function to configure the root directory path.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Function that receives the service provider and returns the root directory path</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddLocalFileMetadataStore(this IServiceCollection services, Func<IServiceProvider, string> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        services.AddSingleton<LocalFileMetadataStore>(provider => {
            var rootDirectoryPath = configure(provider);
            var loggerFactory = provider.GetService<ILoggerFactory>();
            return new(rootDirectoryPath, loggerFactory);
        });

        services.AddSingleton<IFileMetadataStore>(provider => provider.GetRequiredService<LocalFileMetadataStore>());
        return services;
    }

    /// <summary>Adds local file metadata store to the service collection using configuration binding.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="configSectionName">The configuration section name (defaults to LocalFileMetadataStoreOptions.SectionName)</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddLocalFileMetadataStoreFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = LocalFileMetadataStoreOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));

        // Register options
        if (!services.Any(s => s.ServiceType == typeof(LocalFileMetadataStoreOptions))) {
            services.AddSingleton<LocalFileMetadataStoreOptions>(_ => {
                var section = configuration.GetSection(configSectionName);
                var options = new LocalFileMetadataStoreOptions();
                if (section.Exists())
                    section.Bind(options);

                return options;
            });
        }

        services.AddSingleton<LocalFileMetadataStore>(provider => {
            var options = provider.GetRequiredService<LocalFileMetadataStoreOptions>();
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.RootDirectoryPath, nameof(options.RootDirectoryPath));
            var loggerFactory = provider.GetService<ILoggerFactory>();
            return new(options.RootDirectoryPath, loggerFactory);
        });

        services.AddSingleton<IFileMetadataStore>(provider => provider.GetRequiredService<LocalFileMetadataStore>());
        return services;
    }

    /// <summary>Adds a keyed local file metadata store to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="keyName">The key name for the keyed metadata store service</param>
    /// <param name="rootDirectoryPath">The root directory path for storing metadata</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddLocalFileMetadataStoreKeyed(this IServiceCollection services, string keyName, string rootDirectoryPath)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName, nameof(keyName));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(rootDirectoryPath, nameof(rootDirectoryPath));
        services.AddKeyedSingleton<LocalFileMetadataStore>(
            keyName, (provider, _) => {
                var loggerFactory = provider.GetService<ILoggerFactory>();
                return new(rootDirectoryPath, loggerFactory);
            });

        services.AddKeyedSingleton<IFileMetadataStore>(keyName, (provider, _) => provider.GetRequiredKeyedService<LocalFileMetadataStore>(keyName));
        return services;
    }

    /// <summary>Adds a keyed local file metadata store to the service collection using a builder pattern.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="keyName">The key name for the keyed metadata store service</param>
    /// <returns>A builder for configuring the service and its dependencies</returns>
    /// <example>
    /// <code>
    /// // Use configuration section:
    /// services.AddLocalFileMetadataStoreKeyed("local-metadata")
    ///     .ConfigureLocalFileStore("LocalFileMetadataStore")
    ///     .Build();
    /// 
    /// // Use action to configure:
    /// services.AddLocalFileMetadataStoreKeyed("local-metadata")
    ///     .ConfigureLocalFileStore(options => {
    ///         options.RootDirectoryPath = "/path/to/metadata";
    ///         options.CreateDirectoryIfNotExists = true;
    ///     })
    ///     .Build();
    /// </code>
    /// </example>
    public static LocalFileMetadataStoreBuilder AddLocalFileMetadataStoreKeyed(this IServiceCollection services, string keyName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName, nameof(keyName));
        return new(services, keyName);
    }
}