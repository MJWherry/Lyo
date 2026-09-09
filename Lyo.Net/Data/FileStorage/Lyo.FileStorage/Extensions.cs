using Lyo.Configuration;
using Lyo.Common.Core.Extensions;
using Lyo.Compression;
using Lyo.Encryption.TwoKey;
using Lyo.Exceptions;
using Lyo.FileMetadataStore;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Models;
using Lyo.FileStorage.Multipart;
using Lyo.FileStorage.OperationContext;
using Lyo.IO.Temp;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FileStorage;

public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers a singleton <see cref="IFileOperationContextAccessor" /> backed by async-local storage.</summary>
        public IServiceCollection AddFileOperationContextAccessor()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddSingleton<IFileOperationContextAccessor, FileOperationContextAccessor>();
            return services;
        }

        /// <summary>Registers an in-memory multipart session store for single-node work and tests.</summary>
        public IServiceCollection AddInMemoryMultipartUploadSessionStore()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddSingleton<InMemoryMultipartUploadSessionStore>();
            services.AddSingleton<IMultipartUploadSessionStore>(sp => sp.GetRequiredService<InMemoryMultipartUploadSessionStore>());
            return services;
        }

        /// <summary>
        /// Registers <see cref="InMemoryMultipartUploadSessionStore" /> only when no <see cref="IMultipartUploadSessionStore" /> is already present (PostgreSQL file metadata
        /// registration may add <c>PostgresMultipartUploadSessionStore</c> first).
        /// </summary>
        public IServiceCollection TryAddInMemoryMultipartUploadSessionStoreIfMissing()
        {
            ArgumentHelpers.ThrowIfNull(services);
            if (!services.Any(s => s.ServiceType == typeof(IMultipartUploadSessionStore)))
                services.AddInMemoryMultipartUploadSessionStore();

            return services;
        }

        /// <summary>Registers <see cref="LocalMultipartUploadService" /> for server-side multipart uploads staged on local disk.</summary>
        public IServiceCollection AddLocalMultipartUploadService()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddInMemoryMultipartUploadSessionStoreIfMissing();
            services.AddScoped<LocalMultipartUploadService>();
            services.AddScoped<IMultipartUploadService>(sp => sp.GetRequiredService<LocalMultipartUploadService>());
            return services;
        }
    }

    /// <param name="services">DI collection to extend</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers a keyed file storage alias that reuses an existing keyed file storage service and encryption service.</summary>
        /// <param name="keyName">Key for the alias</param>
        /// <param name="fileStoreKeyName">Key of the existing file storage implementation</param>
        /// <param name="encryptionServiceKeyName">Key of the existing encryption service</param>
        /// <returns>The same collection, for chaining</returns>
        public IServiceCollection AddFileStorageServiceKeyed(string keyName, string fileStoreKeyName, string encryptionServiceKeyName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(fileStoreKeyName);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(encryptionServiceKeyName);
            if (keyName == fileStoreKeyName)
                return services;

            services.AddKeyedScoped<IFileStorageService>(keyName, (provider, _) => provider.GetRequiredKeyedService<IFileStorageService>(fileStoreKeyName));
            return services;
        }

        /// <summary>
        /// Registers a keyed file storage alias that reuses an existing keyed file storage service and registers encryption via <paramref name="configEncryptionService" />.
        /// </summary>
        /// <param name="keyName">Key for the alias</param>
        /// <param name="fileStoreKeyName">Key of the existing file storage implementation</param>
        /// <param name="configEncryptionService">Factory that builds the encryption service (registered under <paramref name="keyName" />)</param>
        /// <returns>The same collection, for chaining</returns>
        public IServiceCollection AddFileStorageServiceKeyed(string keyName, string fileStoreKeyName, Func<IServiceProvider, ITwoKeyEncryptionService> configEncryptionService)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(fileStoreKeyName);
            ArgumentHelpers.ThrowIfNull(configEncryptionService);
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(ITwoKeyEncryptionService)))
                services.AddKeyedSingleton<ITwoKeyEncryptionService>(keyName, (provider, _) => configEncryptionService(provider));

            services.AddKeyedScoped<IFileStorageService>(keyName, (provider, _) => provider.GetRequiredKeyedService<IFileStorageService>(fileStoreKeyName));
            return services;
        }

        /// <summary>Registers a keyed file storage service from <paramref name="configFileStore" /> and reuses an existing keyed encryption service.</summary>
        /// <typeparam name="TFileStorageService">Concrete file storage type</typeparam>
        /// <param name="keyName">Key for the file storage service</param>
        /// <param name="configFileStore">Factory that builds the file storage service (registered under <paramref name="keyName" />)</param>
        /// <param name="encryptionServiceKeyName">Key of the existing encryption service</param>
        /// <returns>The same collection, for chaining</returns>
        public IServiceCollection AddFileStorageServiceKeyed<TFileStorageService>(
            string keyName,
            Func<IServiceProvider, TFileStorageService> configFileStore,
            string encryptionServiceKeyName)
            where TFileStorageService : class, IFileStorageService
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(encryptionServiceKeyName);
            ArgumentHelpers.ThrowIfNull(configFileStore);
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(TFileStorageService))) {
                services.AddKeyedScoped<TFileStorageService>(keyName, (provider, _) => configFileStore(provider));
                services.AddKeyedScoped<IFileStorageService>(keyName, (provider, _) => provider.GetRequiredKeyedService<TFileStorageService>(keyName));
            }

            return services;
        }

        /// <summary>Registers a keyed file storage service and a keyed encryption service from the given factories.</summary>
        /// <typeparam name="TFileStorageService">Concrete file storage type</typeparam>
        /// <param name="keyName">Key for both services</param>
        /// <param name="configEncryptionService">Factory that builds the encryption service (registered under <paramref name="keyName" />)</param>
        /// <param name="configFileStore">Factory that builds the file storage service (registered under <paramref name="keyName" />)</param>
        /// <returns>The same collection, for chaining</returns>
        public IServiceCollection AddFileStorageServiceKeyed<TFileStorageService>(
            string keyName,
            Func<IServiceProvider, ITwoKeyEncryptionService> configEncryptionService,
            Func<IServiceProvider, TFileStorageService> configFileStore)
            where TFileStorageService : class, IFileStorageService
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
            ArgumentHelpers.ThrowIfNull(configEncryptionService);
            ArgumentHelpers.ThrowIfNull(configFileStore);
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(ITwoKeyEncryptionService)))
                services.AddKeyedSingleton<ITwoKeyEncryptionService>(keyName, (provider, _) => configEncryptionService(provider));

            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(TFileStorageService))) {
                services.AddKeyedScoped<TFileStorageService>(keyName, (provider, _) => configFileStore(provider));
                services.AddKeyedScoped<IFileStorageService>(keyName, (provider, _) => provider.GetRequiredKeyedService<TFileStorageService>(keyName));
            }

            return services;
        }

        /// <summary>
        /// Registers a keyed local file storage service. Options come from <paramref name="config" />, encryption is an existing keyed service, and metadata comes from
        /// <paramref name="configureMetadataStore" />.
        /// </summary>
        /// <param name="keyName">Key for the file storage service</param>
        /// <param name="config">Callback that fills <see cref="DiskFileStorageOptions" /></param>
        /// <param name="configureMetadataStore">Factory that builds the metadata store</param>
        /// <param name="encryptionServiceKeyName">Key of the existing encryption service</param>
        /// <returns>The same collection, for chaining</returns>
        public IServiceCollection AddFileStorageServiceKeyed(
            string keyName,
            Action<DiskFileStorageOptions> config,
            Func<IServiceProvider, IFileMetadataStore> configureMetadataStore,
            string encryptionServiceKeyName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(encryptionServiceKeyName);
            ArgumentHelpers.ThrowIfNull(config);
            ArgumentHelpers.ThrowIfNull(configureMetadataStore);
            services.AddSingleton<DiskFileStorageOptions>(_ => {
                var options = new DiskFileStorageOptions();
                config(options);
                return options;
            });

            // Register file storage under keyName
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(LocalFileStorageService))) {
                services.AddKeyedScoped<LocalFileStorageService>(
                    keyName, (provider, _) => {
                        var options = provider.GetRequiredService<DiskFileStorageOptions>();
                        var loggerFactory = provider.GetService<ILoggerFactory>();
                        var compressionService = provider.GetService<ICompressionService>();
                        var encryptionService = provider.GetKeyedService<ITwoKeyEncryptionService>(encryptionServiceKeyName);
                        var metadataService = configureMetadataStore(provider);
                        var metrics = options.EnableMetrics ? provider.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
                        return new(options, loggerFactory, compressionService, encryptionService, metadataService, metrics);
                    });

                services.AddKeyedScoped<IFileStorageService>(keyName, (provider, _) => provider.GetRequiredKeyedService<LocalFileStorageService>(keyName));
            }

            return services;
        }

        /// <summary>
        /// Registers a keyed local file storage service. Options come from <paramref name="config" />, encryption from <paramref name="configEncryptionService" />, and metadata
        /// from <paramref name="configureMetadataStore" />.
        /// </summary>
        /// <param name="keyName">Key for the file storage service</param>
        /// <param name="config">Callback that fills <see cref="DiskFileStorageOptions" /></param>
        /// <param name="configureMetadataStore">Factory that builds the metadata store</param>
        /// <param name="configEncryptionService">Factory that builds the encryption service (registered under <paramref name="keyName" />)</param>
        /// <returns>The same collection, for chaining</returns>
        public IServiceCollection AddFileStorageServiceKeyed(
            string keyName,
            Action<DiskFileStorageOptions> config,
            Func<IServiceProvider, IFileMetadataStore> configureMetadataStore,
            Func<IServiceProvider, ITwoKeyEncryptionService> configEncryptionService)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
            ArgumentHelpers.ThrowIfNull(config);
            ArgumentHelpers.ThrowIfNull(configureMetadataStore);
            ArgumentHelpers.ThrowIfNull(configEncryptionService);
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(ITwoKeyEncryptionService)))
                services.AddKeyedSingleton<ITwoKeyEncryptionService>(keyName, (provider, _) => configEncryptionService(provider));

            services.AddSingleton<DiskFileStorageOptions>(_ => {
                var options = new DiskFileStorageOptions();
                config(options);
                return options;
            });

            // Register file storage under keyName
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(LocalFileStorageService))) {
                services.AddKeyedScoped<LocalFileStorageService>(
                    keyName, (provider, _) => {
                        var options = provider.GetRequiredService<DiskFileStorageOptions>();
                        var loggerFactory = provider.GetService<ILoggerFactory>();
                        var compressionService = provider.GetService<ICompressionService>();
                        var encryptionService = provider.GetKeyedService<ITwoKeyEncryptionService>(keyName);
                        var metadataService = configureMetadataStore(provider);
                        var metrics = options.EnableMetrics ? provider.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
                        return new(options, loggerFactory, compressionService, encryptionService, metadataService, metrics);
                    });

                services.AddKeyedScoped<IFileStorageService>(keyName, (provider, _) => provider.GetRequiredKeyedService<LocalFileStorageService>(keyName));
            }

            return services;
        }

        /// <summary>
        /// Registers a keyed local file storage service. Options bind from <paramref name="configSectionName" />, encryption is an existing keyed service, and metadata comes from
        /// <paramref name="configureMetadataStore" />.
        /// </summary>
        /// <param name="keyName">Key for the file storage service</param>
        /// <param name="configSectionName">Configuration section name (for example <c>LocalFileStorageService</c>)</param>
        /// <param name="configureMetadataStore">Factory that builds the metadata store</param>
        /// <param name="encryptionServiceKeyName">Key of the existing encryption service</param>
        /// <returns>The same collection, for chaining</returns>
        public IServiceCollection AddFileStorageServiceKeyed(
            string keyName,
            string configSectionName,
            Func<IServiceProvider, IFileMetadataStore> configureMetadataStore,
            string encryptionServiceKeyName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(encryptionServiceKeyName);
            ArgumentHelpers.ThrowIfNull(configureMetadataStore);

            // Bind options from configuration when they are not already registered
            if (!services.Any(s => s.ServiceType == typeof(DiskFileStorageOptions)))
                services.AddSingleton(provider => DiskFileStorageConfigurationBinder.BindDiskFileStorage(provider, configSectionName));

            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(LocalFileStorageService))) {
                services.AddKeyedScoped<LocalFileStorageService>(
                    keyName, (provider, _) => {
                        var options = provider.GetRequiredService<DiskFileStorageOptions>();
                        var loggerFactory = provider.GetService<ILoggerFactory>();
                        var compressionService = provider.GetService<ICompressionService>();
                        var encryptionService = provider.GetKeyedService<ITwoKeyEncryptionService>(encryptionServiceKeyName);
                        var metadataService = configureMetadataStore(provider);
                        var metrics = options.EnableMetrics ? provider.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
                        return new(options, loggerFactory, compressionService, encryptionService, metadataService, metrics);
                    });

                services.AddKeyedScoped<IFileStorageService>(keyName, (provider, _) => provider.GetRequiredKeyedService<LocalFileStorageService>(keyName));
            }

            return services;
        }

        /// <summary>
        /// Registers a keyed local file storage service. Options bind from <paramref name="configSectionName" />, encryption from <paramref name="configEncryptionService" />, and
        /// metadata from <paramref name="configureMetadataStore" />.
        /// </summary>
        /// <param name="keyName">Key for the file storage service</param>
        /// <param name="configSectionName">Configuration section name (for example <c>LocalFileStorageService</c>)</param>
        /// <param name="configureMetadataStore">Factory that builds the metadata store</param>
        /// <param name="configEncryptionService">Factory that builds the encryption service (registered under <paramref name="keyName" />)</param>
        /// <returns>The same collection, for chaining</returns>
        public IServiceCollection AddFileStorageServiceKeyed(
            string keyName,
            string configSectionName,
            Func<IServiceProvider, IFileMetadataStore> configureMetadataStore,
            Func<IServiceProvider, ITwoKeyEncryptionService> configEncryptionService)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            ArgumentHelpers.ThrowIfNull(configureMetadataStore);
            ArgumentHelpers.ThrowIfNull(configEncryptionService);
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(ITwoKeyEncryptionService)))
                services.AddKeyedSingleton<ITwoKeyEncryptionService>(keyName, (provider, _) => configEncryptionService(provider));

            if (!services.Any(s => s.ServiceType == typeof(DiskFileStorageOptions)))
                services.AddSingleton(provider => DiskFileStorageConfigurationBinder.BindDiskFileStorage(provider, configSectionName));

            // Register file storage under keyName
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(LocalFileStorageService))) {
                services.AddKeyedScoped<LocalFileStorageService>(
                    keyName, (provider, _) => {
                        var options = provider.GetRequiredService<DiskFileStorageOptions>();
                        var loggerFactory = provider.GetService<ILoggerFactory>();
                        var compressionService = provider.GetService<ICompressionService>();
                        var encryptionService = provider.GetKeyedService<ITwoKeyEncryptionService>(keyName);
                        var metadataService = configureMetadataStore(provider);
                        var metrics = options.EnableMetrics ? provider.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
                        return new(options, loggerFactory, compressionService, encryptionService, metadataService, metrics);
                    });

                services.AddKeyedScoped<IFileStorageService>(keyName, (provider, _) => provider.GetRequiredKeyedService<LocalFileStorageService>(keyName));
            }

            return services;
        }

        /// <summary>Registers default <see cref="FileStorageArchiveOptions" /> and <see cref="IIOTempService" /> when they are not already present.</summary>
        public IServiceCollection AddFileStorageArchiveService() => services.AddFileStorageArchiveService(new FileStorageArchiveOptions());

        /// <summary>Registers <see cref="FileStorageArchiveOptions" /> from a callback and <see cref="IIOTempService" /> when they are not already present.</summary>
        public IServiceCollection AddFileStorageArchiveService(Action<FileStorageArchiveOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new FileStorageArchiveOptions();
            configure(options);
            return services.AddFileStorageArchiveService(options);
        }

        /// <summary>Registers <see cref="FileStorageArchiveOptions" /> and <see cref="IIOTempService" /> when they are not already present.</summary>
        public IServiceCollection AddFileStorageArchiveService(FileStorageArchiveOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            if (!services.Any(s => s.ServiceType == typeof(FileStorageArchiveOptions)))
                services.AddSingleton(options);

            services.TryAddIOTempService();
            return services;
        }

        /// <summary>Binds <see cref="FileStorageArchiveOptions" /> from configuration and registers <see cref="IIOTempService" /> when it is not already present.</summary>
        public IServiceCollection AddFileStorageArchiveServiceFromConfiguration(
            IConfiguration configuration,
            string configSectionName = FileStorageArchiveOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = LyoOptions.Bind<FileStorageArchiveOptions>(configuration, configSectionName);

            return services.AddFileStorageArchiveService(options);
        }

        /// <summary>
        /// Registers a keyed <see cref="IFileStorageArchiveService" /> that reads the keyed <see cref="IFileStorageService" />. Call
        /// <see cref="AddFileStorageArchiveServiceFromConfiguration" /> (or another options overload) first so archive caps are bound.
        /// </summary>
        public IServiceCollection AddFileStorageArchiveServiceKeyed(string keyName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
            if (!services.Any(s => s.ServiceType == typeof(FileStorageArchiveOptions)))
                services.AddFileStorageArchiveService();
            else
                services.TryAddIOTempService();

            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(IFileStorageArchiveService))) {
                services.AddKeyedScoped<IFileStorageArchiveService>(
                    keyName, (provider, _) => new FileStorageArchiveService(
                        provider.GetRequiredKeyedService<IFileStorageService>(keyName), provider.GetRequiredService<IIOTempService>(),
                        provider.GetRequiredService<FileStorageArchiveOptions>(), provider.GetService<ILogger<FileStorageArchiveService>>()));
            }

            return services;
        }

        private void TryAddIOTempService()
        {
            if (!services.Any(s => s.ServiceType == typeof(IIOTempService)))
                services.AddIOTempService();
        }
    }

    internal static class DiskFileStorageConfigurationBinder
    {
        public static DiskFileStorageOptions BindDiskFileStorage(IServiceProvider provider, string? preferredSection)
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var options = new DiskFileStorageOptions();
            var lf = provider.GetService<ILoggerFactory>();
            foreach (var name in OrderedSectionCandidates(preferredSection)) {
                var section = configuration.GetSection(name);
                if (!section.Exists())
                    continue;

                section.Bind(options);
                if (string.Equals(name, DiskFileStorageOptions.LegacySectionName, StringComparison.Ordinal)) {
                    (lf ?? NullLoggerFactory.Instance).CreateLogger("Lyo.FileStorage.Disk")
                        .LogWarning(
                            "Disk file storage options were loaded from legacy configuration section [{Legacy}]; migrate appsettings to [{Current}].",
                            DiskFileStorageOptions.LegacySectionName, DiskFileStorageOptions.SectionName);
                }

                return options;
            }

            return options;
        }

        private static IEnumerable<string> OrderedSectionCandidates(string? preferredSection)
        {
            if (!preferredSection.IsNullOrWhitespace())
                yield return preferredSection.Trim();

            if (!string.Equals(preferredSection, DiskFileStorageOptions.SectionName, StringComparison.Ordinal))
                yield return DiskFileStorageOptions.SectionName;

            if (!string.Equals(preferredSection, DiskFileStorageOptions.LegacySectionName, StringComparison.Ordinal))
                yield return DiskFileStorageOptions.LegacySectionName;
        }
    }
}