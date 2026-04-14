using Lyo.Compression;
using Lyo.Encryption.TwoKey;
using Lyo.Exceptions;
using Lyo.FileMetadataStore;
using Lyo.FileStorage.Models;
using Lyo.FileStorage.Multipart;
using Lyo.FileStorage.OperationContext;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.FileStorage;

public static class Extensions
{
    /// <summary>Registers a singleton <see cref="IFileOperationContextAccessor" /> using async-local storage.</summary>
    public static IServiceCollection AddFileOperationContextAccessor(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        services.AddSingleton<IFileOperationContextAccessor, FileOperationContextAccessor>();
        return services;
    }

    /// <summary>Registers an in-memory multipart session store (single-node / tests).</summary>
    public static IServiceCollection AddInMemoryMultipartUploadSessionStore(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        services.AddSingleton<InMemoryMultipartUploadSessionStore>();
        services.AddSingleton<IMultipartUploadSessionStore>(sp => sp.GetRequiredService<InMemoryMultipartUploadSessionStore>());
        return services;
    }

    /// <summary>
    /// Registers <see cref="InMemoryMultipartUploadSessionStore" /> only when no <see cref="IMultipartUploadSessionStore" /> is already registered (for example PostgreSQL file
    /// metadata registration may add <c>PostgresMultipartUploadSessionStore</c> first).
    /// </summary>
    public static IServiceCollection TryAddInMemoryMultipartUploadSessionStoreIfMissing(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        if (!services.Any(s => s.ServiceType == typeof(IMultipartUploadSessionStore)))
            services.AddInMemoryMultipartUploadSessionStore();

        return services;
    }

    /// <summary>Registers <see cref="LocalMultipartUploadService" /> for server-side multipart uploads to local disk staging.</summary>
    public static IServiceCollection AddLocalMultipartUploadService(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        services.TryAddInMemoryMultipartUploadSessionStoreIfMissing();
        services.AddScoped<LocalMultipartUploadService>();
        services.AddScoped<IMultipartUploadService>(sp => sp.GetRequiredService<LocalMultipartUploadService>());
        return services;
    }

    /// <param name="services">The service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds a keyed file storage service to the service collection. Uses existing keyed file storage service and encryption service.</summary>
        /// <param name="keyName">The key name for the keyed file storage service</param>
        /// <param name="fileStoreKeyName">The key name for the keyed file storage service implementation</param>
        /// <param name="encryptionServiceKeyName">The key name for the keyed encryption service</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddFileStorageServiceKeyed(string keyName, string fileStoreKeyName, string encryptionServiceKeyName)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName, nameof(keyName));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(fileStoreKeyName, nameof(fileStoreKeyName));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(encryptionServiceKeyName, nameof(encryptionServiceKeyName));
            if (keyName == fileStoreKeyName)
                return services;

            services.AddKeyedScoped<IFileStorageService>(keyName, (provider, _) => provider.GetRequiredKeyedService<IFileStorageService>(fileStoreKeyName));
            return services;
        }

        /// <summary>Adds a keyed file storage service to the service collection. Uses existing keyed file storage service and configures encryption service.</summary>
        /// <param name="keyName">The key name for the keyed file storage service</param>
        /// <param name="fileStoreKeyName">The key name for the keyed file storage service implementation</param>
        /// <param name="configEncryptionService">Function to configure the encryption service (will be registered with keyName)</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddFileStorageServiceKeyed(string keyName, string fileStoreKeyName, Func<IServiceProvider, ITwoKeyEncryptionService> configEncryptionService)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName, nameof(keyName));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(fileStoreKeyName, nameof(fileStoreKeyName));
            ArgumentHelpers.ThrowIfNull(configEncryptionService, nameof(configEncryptionService));
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(ITwoKeyEncryptionService)))
                services.AddKeyedSingleton<ITwoKeyEncryptionService>(keyName, (provider, _) => configEncryptionService(provider));

            services.AddKeyedScoped<IFileStorageService>(keyName, (provider, _) => provider.GetRequiredKeyedService<IFileStorageService>(fileStoreKeyName));
            return services;
        }

        /// <summary>Adds a keyed file storage service to the service collection. Configures file storage service and uses existing keyed encryption service.</summary>
        /// <typeparam name="TFileStorageService">The file storage service type</typeparam>
        /// <param name="keyName">The key name for the keyed file storage service</param>
        /// <param name="configFileStore">Function to configure the file storage service (will be registered with keyName)</param>
        /// <param name="encryptionServiceKeyName">The key name for the keyed encryption service</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddFileStorageServiceKeyed<TFileStorageService>(
            string keyName,
            Func<IServiceProvider, TFileStorageService> configFileStore,
            string encryptionServiceKeyName)
            where TFileStorageService : class, IFileStorageService
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName, nameof(keyName));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(encryptionServiceKeyName, nameof(encryptionServiceKeyName));
            ArgumentHelpers.ThrowIfNull(configFileStore, nameof(configFileStore));
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(TFileStorageService))) {
                services.AddKeyedScoped<TFileStorageService>(keyName, (provider, _) => configFileStore(provider));
                services.AddKeyedScoped<IFileStorageService>(keyName, (provider, _) => provider.GetRequiredKeyedService<TFileStorageService>(keyName));
            }

            return services;
        }

        /// <summary>Adds a keyed file storage service to the service collection. Configures both file storage service and encryption service.</summary>
        /// <typeparam name="TFileStorageService">The file storage service type</typeparam>
        /// <param name="keyName">The key name for the keyed file storage service</param>
        /// <param name="configEncryptionService">Function to configure the encryption service (will be registered with keyName)</param>
        /// <param name="configFileStore">Function to configure the file storage service (will be registered with keyName)</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddFileStorageServiceKeyed<TFileStorageService>(
            string keyName,
            Func<IServiceProvider, ITwoKeyEncryptionService> configEncryptionService,
            Func<IServiceProvider, TFileStorageService> configFileStore)
            where TFileStorageService : class, IFileStorageService
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName, nameof(keyName));
            ArgumentHelpers.ThrowIfNull(configEncryptionService, nameof(configEncryptionService));
            ArgumentHelpers.ThrowIfNull(configFileStore, nameof(configFileStore));
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(ITwoKeyEncryptionService)))
                services.AddKeyedSingleton<ITwoKeyEncryptionService>(keyName, (provider, _) => configEncryptionService(provider));

            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(TFileStorageService))) {
                services.AddKeyedScoped<TFileStorageService>(keyName, (provider, _) => configFileStore(provider));
                services.AddKeyedScoped<IFileStorageService>(keyName, (provider, _) => provider.GetRequiredKeyedService<TFileStorageService>(keyName));
            }

            return services;
        }

        /// <summary>
        /// Adds a keyed local file storage service to the service collection. Configures file storage service from action or config section, uses existing keyed encryption service,
        /// and configures metadata store.
        /// </summary>
        /// <param name="keyName">The key name for the keyed file storage service</param>
        /// <param name="config">Action to configure the options, or config section name string</param>
        /// <param name="configureMetadataStore">Function to configure the metadata store</param>
        /// <param name="encryptionServiceKeyName">The key name for the keyed encryption service, or function to configure encryption service</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddFileStorageServiceKeyed(
            string keyName,
            Action<LocalFileStorageServiceOptions> config,
            Func<IServiceProvider, IFileMetadataStore> configureMetadataStore,
            string encryptionServiceKeyName)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName, nameof(keyName));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(encryptionServiceKeyName, nameof(encryptionServiceKeyName));
            ArgumentHelpers.ThrowIfNull(config, nameof(config));
            ArgumentHelpers.ThrowIfNull(configureMetadataStore, nameof(configureMetadataStore));
            services.AddSingleton<LocalFileStorageServiceOptions>(_ => {
                var options = new LocalFileStorageServiceOptions();
                config(options);
                return options;
            });

            // Register file storage service with keyName
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(LocalFileStorageService))) {
                services.AddKeyedScoped<LocalFileStorageService>(
                    keyName, (provider, _) => {
                        var options = provider.GetRequiredService<LocalFileStorageServiceOptions>();
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
        /// Adds a keyed local file storage service to the service collection. Configures file storage service from action or config section, configures encryption service, and
        /// configures metadata store.
        /// </summary>
        /// <param name="keyName">The key name for the keyed file storage service</param>
        /// <param name="config">Action to configure the options, or config section name string</param>
        /// <param name="configureMetadataStore">Function to configure the metadata store</param>
        /// <param name="configEncryptionService">Function to configure the encryption service (will be registered with keyName)</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddFileStorageServiceKeyed(
            string keyName,
            Action<LocalFileStorageServiceOptions> config,
            Func<IServiceProvider, IFileMetadataStore> configureMetadataStore,
            Func<IServiceProvider, ITwoKeyEncryptionService> configEncryptionService)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName, nameof(keyName));
            ArgumentHelpers.ThrowIfNull(config, nameof(config));
            ArgumentHelpers.ThrowIfNull(configureMetadataStore, nameof(configureMetadataStore));
            ArgumentHelpers.ThrowIfNull(configEncryptionService, nameof(configEncryptionService));
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(ITwoKeyEncryptionService)))
                services.AddKeyedSingleton<ITwoKeyEncryptionService>(keyName, (provider, _) => configEncryptionService(provider));

            services.AddSingleton<LocalFileStorageServiceOptions>(_ => {
                var options = new LocalFileStorageServiceOptions();
                config(options);
                return options;
            });

            // Register file storage service with keyName
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(LocalFileStorageService))) {
                services.AddKeyedScoped<LocalFileStorageService>(
                    keyName, (provider, _) => {
                        var options = provider.GetRequiredService<LocalFileStorageServiceOptions>();
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
        /// Adds a keyed local file storage service to the service collection. Configures file storage service from config section, uses existing keyed encryption service, and
        /// configures metadata store.
        /// </summary>
        /// <param name="keyName">The key name for the keyed file storage service</param>
        /// <param name="configSectionName">The configuration section name (e.g., "LocalFileStorageService")</param>
        /// <param name="configureMetadataStore">Function to configure the metadata store</param>
        /// <param name="encryptionServiceKeyName">The key name for the keyed encryption service, or function to configure encryption service</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddFileStorageServiceKeyed(
            string keyName,
            string configSectionName,
            Func<IServiceProvider, IFileMetadataStore> configureMetadataStore,
            string encryptionServiceKeyName)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName, nameof(keyName));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(encryptionServiceKeyName, nameof(encryptionServiceKeyName));
            ArgumentHelpers.ThrowIfNull(configureMetadataStore, nameof(configureMetadataStore));

            // Configure options from configuration (if not already registered)
            if (!services.Any(s => s.ServiceType == typeof(LocalFileStorageServiceOptions))) {
                services.AddSingleton<LocalFileStorageServiceOptions>(provider => {
                    var configuration = provider.GetRequiredService<IConfiguration>();
                    var section = configuration.GetSection(configSectionName);
                    var options = new LocalFileStorageServiceOptions();
                    if (!section.Exists())
                        return options;

                    section.Bind(options);
                    return options;
                });
            }

            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(LocalFileStorageService))) {
                services.AddKeyedScoped<LocalFileStorageService>(
                    keyName, (provider, _) => {
                        var options = provider.GetRequiredService<LocalFileStorageServiceOptions>();
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
        /// Adds a keyed local file storage service to the service collection. Configures file storage service from config section, configures encryption service, and configures
        /// metadata store.
        /// </summary>
        /// <param name="keyName">The key name for the keyed file storage service</param>
        /// <param name="configSectionName">The configuration section name (e.g., "LocalFileStorageService")</param>
        /// <param name="configureMetadataStore">Function to configure the metadata store</param>
        /// <param name="configEncryptionService">Function to configure the encryption service (will be registered with keyName)</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddFileStorageServiceKeyed(
            string keyName,
            string configSectionName,
            Func<IServiceProvider, IFileMetadataStore> configureMetadataStore,
            Func<IServiceProvider, ITwoKeyEncryptionService> configEncryptionService)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName, nameof(keyName));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
            ArgumentHelpers.ThrowIfNull(configureMetadataStore, nameof(configureMetadataStore));
            ArgumentHelpers.ThrowIfNull(configEncryptionService, nameof(configEncryptionService));
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(ITwoKeyEncryptionService)))
                services.AddKeyedSingleton<ITwoKeyEncryptionService>(keyName, (provider, _) => configEncryptionService(provider));

            if (!services.Any(s => s.ServiceType == typeof(LocalFileStorageServiceOptions))) {
                services.AddSingleton<LocalFileStorageServiceOptions>(provider => {
                    var configuration = provider.GetRequiredService<IConfiguration>();
                    var section = configuration.GetSection(configSectionName);
                    var options = new LocalFileStorageServiceOptions();
                    if (!section.Exists())
                        return options;

                    section.Bind(options);
                    return options;
                });
            }

            // Register file storage service with keyName
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(LocalFileStorageService))) {
                services.AddKeyedScoped<LocalFileStorageService>(
                    keyName, (provider, _) => {
                        var options = provider.GetRequiredService<LocalFileStorageServiceOptions>();
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
    }
}