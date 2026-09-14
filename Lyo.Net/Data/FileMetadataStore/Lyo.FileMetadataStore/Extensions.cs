using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.FileMetadataStore;

public static class Extensions
{
    /// <param name="services">Service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds the local file metadata store.</summary>
        /// <param name="rootDirectoryPath">Root directory for metadata files.</param>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddLocalFileMetadataStore(string rootDirectoryPath)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(rootDirectoryPath);
            services.AddSingleton<LocalFileMetadataStore>(provider => {
                var loggerFactory = provider.GetService<ILoggerFactory>();
                return new(rootDirectoryPath, loggerFactory);
            });

            services.AddSingleton<IFileMetadataStore>(provider => provider.GetRequiredService<LocalFileMetadataStore>());
            return services;
        }

        /// <summary>Adds the local file metadata store using a factory for the root directory path.</summary>
        /// <param name="configure">Receives the service provider and returns the root directory path.</param>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddLocalFileMetadataStore(Func<IServiceProvider, string> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddSingleton<LocalFileMetadataStore>(provider => {
                var rootDirectoryPath = configure(provider);
                var loggerFactory = provider.GetService<ILoggerFactory>();
                return new(rootDirectoryPath, loggerFactory);
            });

            services.AddSingleton<IFileMetadataStore>(provider => provider.GetRequiredService<LocalFileMetadataStore>());
            return services;
        }

        /// <summary>Adds the local file metadata store from configuration (section <see cref="LocalFileMetadataStoreOptions.SectionName" /> when omitted).</summary>
        /// <param name="configuration">Configuration root.</param>
        /// <param name="configSectionName">Section name; starts as LocalFileMetadataStoreOptions.SectionName.</param>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddLocalFileMetadataStoreFromConfiguration(IConfiguration configuration, string configSectionName = LocalFileMetadataStoreOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);

            // Register options when missing
            if (!services.Any(s => s.ServiceType == typeof(LocalFileMetadataStoreOptions))) {
                var options = new LocalFileMetadataStoreOptions();
                configuration.GetSection(configSectionName).Bind(options);
                options.Validate();
                services.AddSingleton(options);
            }

            services.AddSingleton<LocalFileMetadataStore>(provider => {
                var options = provider.GetRequiredService<LocalFileMetadataStoreOptions>();
                var loggerFactory = provider.GetService<ILoggerFactory>();
                return new(options.RootDirectoryPath, loggerFactory);
            });

            services.AddSingleton<IFileMetadataStore>(provider => provider.GetRequiredService<LocalFileMetadataStore>());
            return services;
        }

        /// <summary>Adds a keyed local file metadata store.</summary>
        /// <param name="keyName">DI key.</param>
        /// <param name="rootDirectoryPath">Root directory for metadata files.</param>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddLocalFileMetadataStoreKeyed(string keyName, string rootDirectoryPath)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(rootDirectoryPath);
            services.AddKeyedSingleton<LocalFileMetadataStore>(
                keyName, (provider, _) => {
                    var loggerFactory = provider.GetService<ILoggerFactory>();
                    return new(rootDirectoryPath, loggerFactory);
                });

            services.AddKeyedSingleton<IFileMetadataStore>(keyName, (provider, _) => provider.GetRequiredKeyedService<LocalFileMetadataStore>(keyName));
            return services;
        }

        /// <summary>Adds a keyed local file metadata store and returns a builder for further setup.</summary>
        /// <param name="keyName">DI key.</param>
        /// <returns>Builder for configuring the store and its dependencies.</returns>
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
        public LocalFileMetadataStoreBuilder AddLocalFileMetadataStoreKeyed(string keyName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
            return new(services, keyName);
        }
    }
}