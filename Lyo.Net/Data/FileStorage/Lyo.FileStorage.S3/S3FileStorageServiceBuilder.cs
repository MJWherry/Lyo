using Amazon.S3;
using Lyo.Compression;
using Lyo.Encryption.TwoKey;
using Lyo.Exceptions;
using Lyo.FileMetadataStore;
using Lyo.FileStorage;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Multipart;
using Lyo.FileStorage.S3.Multipart;
using Lyo.FileStorage.OperationContext;
using Lyo.FileStorage.Policy;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.FileStorage.S3;

/// <summary>
/// Builder for configuring S3 file storage and its dependencies. <see cref="Build" /> also ensures an <see cref="IMultipartUploadSessionStore" />
/// is registered when missing (in-memory fallback), and registers keyed <see cref="S3MultipartUploadService" /> for the same key when not already present.
/// Usage examples: // Use existing keyed services: services.AddS3FileStorageServiceKeyed("client-files")
/// .UseFileMetadataStore("postgres-filemetadatastore") .UseEncryptionService("two-key-aws") .ConfigureS3FileStorage("S3FileStorageOptions") .Build(configuration); // Use factory
/// functions: services.AddS3FileStorageServiceKeyed("client-files") .ConfigureFileMetadataStore(provider => new LocalFileMetadataStore("/path")) .ConfigureEncryptionService(provider
/// => provider.GetRequiredService
/// <ITwoKeyEncryptionService>
/// ()) .ConfigureS3FileStorage(options => { options.BucketName = "my-bucket"; }) .Build(configuration); // Mix and match:
/// services.AddS3FileStorageServiceKeyed("client-files") .UseFileMetadataStore("postgres-filemetadatastore") .ConfigureEncryptionService(provider =>
/// CreateEncryptionService(provider)) .ConfigureS3FileStorage() // Uses default config section .Build(configuration);
/// </summary>
public sealed class S3FileStorageServiceBuilder
{
    private readonly string _keyName;
    private readonly IServiceCollection _services;
    private string? _s3FileStorageConfigSection;
    private Action<S3FileStorageOptions>? _s3FileStorageConfigure;
    private string? _encryptionServiceConfigSection;
    private Func<IServiceProvider, ITwoKeyEncryptionService>? _encryptionServiceFactory;
    private string? _encryptionServiceKeyName;
    private string? _keyStoreConfigSection;
    private string? _keyStoreKeyName;
    private string? _metadataStoreConfigSection;
    private Func<IServiceProvider, IFileMetadataStore>? _metadataStoreFactory;
    private string? _metadataStoreKeyName;

    internal S3FileStorageServiceBuilder(IServiceCollection services, string keyName)
    {
        _services = ArgumentHelpers.ThrowIfNullReturn(services, nameof(services));
        _keyName = ArgumentHelpers.ThrowIfNullReturn(keyName, nameof(keyName));
    }

    /// <summary>Configures the key store using a configuration section name.</summary>
    /// <param name="configSectionName">The configuration section name</param>
    /// <returns>The builder for chaining</returns>
    public S3FileStorageServiceBuilder ConfigureKeyStore(string configSectionName)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        _keyStoreConfigSection = configSectionName;
        return this;
    }

    /// <summary>Configures the key store using a keyed service name.</summary>
    /// <param name="keyName">The keyed service name</param>
    /// <returns>The builder for chaining</returns>
    public S3FileStorageServiceBuilder UseKeyStore(string keyName)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName, nameof(keyName));
        _keyStoreKeyName = keyName;
        return this;
    }

    /// <summary>Configures the file metadata store using a configuration section name.</summary>
    /// <param name="configSectionName">The configuration section name</param>
    /// <returns>The builder for chaining</returns>
    public S3FileStorageServiceBuilder ConfigureFileMetadataStore(string configSectionName)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        _metadataStoreConfigSection = configSectionName;
        return this;
    }

    /// <summary>Configures the file metadata store using a keyed service name.</summary>
    /// <param name="keyName">The keyed service name</param>
    /// <returns>The builder for chaining</returns>
    public S3FileStorageServiceBuilder UseFileMetadataStore(string keyName)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName, nameof(keyName));
        _metadataStoreKeyName = keyName;
        return this;
    }

    /// <summary>Configures the file metadata store using a factory function.</summary>
    /// <param name="factory">Factory function to create the metadata store</param>
    /// <returns>The builder for chaining</returns>
    public S3FileStorageServiceBuilder ConfigureFileMetadataStore(Func<IServiceProvider, IFileMetadataStore> factory)
    {
        ArgumentHelpers.ThrowIfNull(factory, nameof(factory));
        _metadataStoreFactory = factory;
        return this;
    }

    /// <summary>Configures the encryption service using a configuration section name.</summary>
    /// <param name="configSectionName">The configuration section name</param>
    /// <returns>The builder for chaining</returns>
    public S3FileStorageServiceBuilder ConfigureEncryptionService(string configSectionName)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        _encryptionServiceConfigSection = configSectionName;
        return this;
    }

    /// <summary>Configures the encryption service using a keyed service name.</summary>
    /// <param name="keyName">The keyed service name</param>
    /// <returns>The builder for chaining</returns>
    public S3FileStorageServiceBuilder UseEncryptionService(string keyName)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName, nameof(keyName));
        _encryptionServiceKeyName = keyName;
        return this;
    }

    /// <summary>Configures the encryption service using a factory function.</summary>
    /// <param name="factory">Factory function to create the encryption service</param>
    /// <returns>The builder for chaining</returns>
    public S3FileStorageServiceBuilder ConfigureEncryptionService(Func<IServiceProvider, ITwoKeyEncryptionService> factory)
    {
        ArgumentHelpers.ThrowIfNull(factory, nameof(factory));
        _encryptionServiceFactory = factory;
        return this;
    }

    /// <summary>Configures AWS file storage options using a configuration section name.</summary>
    /// <param name="configSectionName">The configuration section name (defaults to S3FileStorageOptions.SectionName)</param>
    /// <returns>The builder for chaining</returns>
    public S3FileStorageServiceBuilder ConfigureS3FileStorage(string configSectionName = S3FileStorageOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        _s3FileStorageConfigSection = configSectionName;
        return this;
    }

    /// <summary>Configures AWS file storage options using an action.</summary>
    /// <param name="configure">Action to configure the options</param>
    /// <returns>The builder for chaining</returns>
    public S3FileStorageServiceBuilder ConfigureS3FileStorage(Action<S3FileStorageOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        _s3FileStorageConfigure = configure;
        return this;
    }

    /// <summary>Builds and registers the AWS file storage service.</summary>
    /// <param name="configuration">Application configuration (required when using configuration-based AWS/S3 registration).</param>
    /// <returns>The service collection for chaining</returns>
    public IServiceCollection Build(IConfiguration configuration)
    {
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));

        // Configure AWS File Storage Options
        var configSectionName = _s3FileStorageConfigSection ?? S3FileStorageOptions.SectionName;
        if (!_services.Any(s => s.ServiceType == typeof(S3FileStorageOptions))) {
            if (_s3FileStorageConfigure != null) {
                _services.AddSingleton<S3FileStorageOptions>(_ => {
                    var options = new S3FileStorageOptions();
                    _s3FileStorageConfigure(options);
                    return options;
                });
            }
            else {
                _services.AddSingleton<S3FileStorageOptions>(_ => {
                    var section = configuration.GetSection(configSectionName);
                    var options = new S3FileStorageOptions();
                    if (section.Exists())
                        section.Bind(options);

                    return options;
                });
            }
        }

        // Register IAmazonS3 if not already registered
        if (!_services.Any(s => s.ServiceType == typeof(IAmazonS3)))
            _services.AddAmazonS3FromConfiguration(configuration, configSectionName);

        // Configure KeyStore if specified via config section
        // Note: KeyStore registration would need to be done via its extension methods
        // This builder assumes KeyStore is already registered or will be registered separately

        // Configure Encryption Service
        string? encryptionServiceKeyToUse = null;
        if (!string.IsNullOrWhiteSpace(_encryptionServiceKeyName)) {
            // Use existing keyed encryption service
            encryptionServiceKeyToUse = _encryptionServiceKeyName;
        }
        else if (!string.IsNullOrWhiteSpace(_encryptionServiceConfigSection)) {
            // Register encryption service from config section using default key name
            // This would typically call AddTwoKeyEncryptionServiceKeyed with the config section
            // For now, we'll use the builder's key name
            encryptionServiceKeyToUse = _keyName;
            // Note: Actual registration would need to be done via extension methods
            // This is a placeholder - the user should register encryption service separately
        }
        else if (_encryptionServiceFactory != null) {
            // Register encryption service from factory
            if (!_services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(_keyName) && s.ServiceType == typeof(ITwoKeyEncryptionService)))
                _services.AddKeyedSingleton<ITwoKeyEncryptionService>(_keyName, (provider, _) => _encryptionServiceFactory(provider));

            encryptionServiceKeyToUse = _keyName;
        }

        // Configure File Metadata Store
        string? metadataStoreKeyToUse = null;
        if (!string.IsNullOrWhiteSpace(_metadataStoreKeyName)) {
            // Use existing keyed metadata store
            metadataStoreKeyToUse = _metadataStoreKeyName;
        }
        else if (!string.IsNullOrWhiteSpace(_metadataStoreConfigSection)) {
            // Register from config section - would use AddPostgresFileMetadataStoreKeyed
            // For now, use default key name
            metadataStoreKeyToUse = _keyName;
            // Note: Actual registration would need to be done via extension methods
        }

        // Register AWS File Storage Service as scoped to match the scoped metadata store
        _services.AddKeyedScoped<S3FileStorageService>(
            _keyName, (provider, _) => {
                var options = provider.GetRequiredService<S3FileStorageOptions>();
                var loggerFactory = provider.GetService<ILoggerFactory>();
                IFileMetadataStore metadataService;
                if (!string.IsNullOrWhiteSpace(metadataStoreKeyToUse))
                    metadataService = provider.GetRequiredKeyedService<IFileMetadataStore>(metadataStoreKeyToUse);
                else if (_metadataStoreFactory != null)
                    metadataService = _metadataStoreFactory(provider);
                else
                    metadataService = provider.GetRequiredService<IFileMetadataStore>();

                var compressionService = provider.GetService<ICompressionService>();
                ITwoKeyEncryptionService? encryptionService = null;
                if (!string.IsNullOrWhiteSpace(encryptionServiceKeyToUse))
                    encryptionService = provider.GetKeyedService<ITwoKeyEncryptionService>(encryptionServiceKeyToUse);
                else if (_encryptionServiceFactory != null)
                    encryptionService = _encryptionServiceFactory(provider);
                else
                    encryptionService = provider.GetService<ITwoKeyEncryptionService>();

                var s3Client = provider.GetService<IAmazonS3>();
                var metrics = options.EnableMetrics ? provider.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
                var operationContextAccessor = provider.GetService<IFileOperationContextAccessor>();
                var auditHandlers = provider.GetServices<IFileAuditEventHandler>();
                var contentPolicy = provider.GetService<IFileContentPolicy>();
                var malwareScanner = provider.GetService<IFileMalwareScanner>();
                return new(
                    options, metadataService, loggerFactory, compressionService, encryptionService, s3Client, metrics, operationContextAccessor, auditHandlers, contentPolicy,
                    malwareScanner);
            });

        _services.AddKeyedScoped<IFileStorageService>(
            _keyName,
            (provider, _) => provider.GetRequiredKeyedService<S3FileStorageService>(_keyName) ??
                throw new InvalidOperationException($"Keyed S3 file storage service '{_keyName}' was not found."));

        _services.TryAddInMemoryMultipartUploadSessionStoreIfMissing();

        // S3-compatible multipart uses the same IAmazonS3 client and API as single-part storage; register unless already present.
        if (!_services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(_keyName) && s.ServiceType == typeof(S3MultipartUploadService)))
            _services.AddKeyedS3MultipartUploadService(_keyName);

        return _services;
    }
}