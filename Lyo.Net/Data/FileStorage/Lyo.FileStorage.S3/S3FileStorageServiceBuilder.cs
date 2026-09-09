using Amazon.S3;
using Lyo.Configuration;
using Lyo.Compression;
using Lyo.Encryption.TwoKey;
using Lyo.Exceptions;
using Lyo.FileMetadataStore;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Multipart;
using Lyo.FileStorage.OperationContext;
using Lyo.FileStorage.Policy;
using Lyo.FileStorage.S3.Multipart;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.FileStorage.S3;

/// <summary>
/// Fluent setup for keyed S3 file storage. <see cref="Build" /> adds an in-memory <see cref="IMultipartUploadSessionStore" /> when none exists, and a keyed
/// <see cref="S3MultipartUploadService" /> for the same key when missing. Bind a section via <c>ConfigureS3FileStorage("S3FileStorageOptions")</c>, or mutate options in
/// <c>ConfigureS3FileStorage(options =&gt; …)</c>, then call <c>Build(configuration)</c>. Point at existing keyed stores with <c>UseFileMetadataStore</c> /
/// <c>UseEncryptionService</c>, or supply factories via <c>ConfigureFileMetadataStore</c> / <c>ConfigureEncryptionService</c>.
/// </summary>
public sealed class S3FileStorageServiceBuilder
{
    private readonly string _keyName;
    private readonly IServiceCollection _services;
    private string? _encryptionServiceConfigSection;
    private Func<IServiceProvider, ITwoKeyEncryptionService>? _encryptionServiceFactory;
    private string? _encryptionServiceKeyName;
    private string? _keyStoreConfigSection;
    private string? _keyStoreKeyName;
    private string? _metadataStoreConfigSection;
    private Func<IServiceProvider, IFileMetadataStore>? _metadataStoreFactory;
    private string? _metadataStoreKeyName;
    private string? _s3FileStorageConfigSection;
    private Action<S3FileStorageOptions>? _s3FileStorageConfigure;

    internal S3FileStorageServiceBuilder(IServiceCollection services, string keyName)
    {
        _services = ArgumentHelpers.ThrowIfNullReturn(services);
        _keyName = ArgumentHelpers.ThrowIfNullReturn(keyName);
    }

    /// <summary>Binds the key store from a configuration section.</summary>
    /// <param name="configSectionName">Section name.</param>
    /// <returns>This builder for chaining.</returns>
    public S3FileStorageServiceBuilder ConfigureKeyStore(string configSectionName)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
        _keyStoreConfigSection = configSectionName;
        return this;
    }

    /// <summary>Resolves the key store from a keyed DI registration.</summary>
    /// <param name="keyName">DI key.</param>
    /// <returns>This builder for chaining.</returns>
    public S3FileStorageServiceBuilder UseKeyStore(string keyName)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
        _keyStoreKeyName = keyName;
        return this;
    }

    /// <summary>Binds the file metadata store from a configuration section.</summary>
    /// <param name="configSectionName">Section name.</param>
    /// <returns>This builder for chaining.</returns>
    public S3FileStorageServiceBuilder ConfigureFileMetadataStore(string configSectionName)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
        _metadataStoreConfigSection = configSectionName;
        return this;
    }

    /// <summary>Resolves the file metadata store from a keyed DI registration.</summary>
    /// <param name="keyName">DI key.</param>
    /// <returns>This builder for chaining.</returns>
    public S3FileStorageServiceBuilder UseFileMetadataStore(string keyName)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
        _metadataStoreKeyName = keyName;
        return this;
    }

    /// <summary>Builds the file metadata store from a factory.</summary>
    /// <param name="factory">Callback that creates the metadata store.</param>
    /// <returns>This builder for chaining.</returns>
    public S3FileStorageServiceBuilder ConfigureFileMetadataStore(Func<IServiceProvider, IFileMetadataStore> factory)
    {
        ArgumentHelpers.ThrowIfNull(factory);
        _metadataStoreFactory = factory;
        return this;
    }

    /// <summary>Binds the encryption service from a configuration section.</summary>
    /// <param name="configSectionName">Section name.</param>
    /// <returns>This builder for chaining.</returns>
    public S3FileStorageServiceBuilder ConfigureEncryptionService(string configSectionName)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
        _encryptionServiceConfigSection = configSectionName;
        return this;
    }

    /// <summary>Resolves the encryption service from a keyed DI registration.</summary>
    /// <param name="keyName">DI key.</param>
    /// <returns>This builder for chaining.</returns>
    public S3FileStorageServiceBuilder UseEncryptionService(string keyName)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
        _encryptionServiceKeyName = keyName;
        return this;
    }

    /// <summary>Builds the encryption service from a factory.</summary>
    /// <param name="factory">Callback that creates the encryption service.</param>
    /// <returns>This builder for chaining.</returns>
    public S3FileStorageServiceBuilder ConfigureEncryptionService(Func<IServiceProvider, ITwoKeyEncryptionService> factory)
    {
        ArgumentHelpers.ThrowIfNull(factory);
        _encryptionServiceFactory = factory;
        return this;
    }

    /// <summary>Binds S3 file storage options from a configuration section.</summary>
    /// <param name="configSectionName">Section name; starts as S3FileStorageOptions.SectionName.</param>
    /// <returns>This builder for chaining.</returns>
    public S3FileStorageServiceBuilder ConfigureS3FileStorage(string configSectionName = S3FileStorageOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
        _s3FileStorageConfigSection = configSectionName;
        return this;
    }

    /// <summary>Mutates S3 file storage options via a callback.</summary>
    /// <param name="configure">Callback that sets options.</param>
    /// <returns>This builder for chaining.</returns>
    public S3FileStorageServiceBuilder ConfigureS3FileStorage(Action<S3FileStorageOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        _s3FileStorageConfigure = configure;
        return this;
    }

    /// <summary>Registers the S3 file storage service and returns the service collection.</summary>
    /// <param name="configuration">Host configuration; required when options are bound from a section.</param>
    /// <returns>Service collection for chaining.</returns>
    public IServiceCollection Build(IConfiguration configuration)
    {
        ArgumentHelpers.ThrowIfNull(configuration);

        // Bind or apply S3 options
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
                    var options = LyoOptions.Bind<S3FileStorageOptions>(configuration, configSectionName);

                    return options;
                });
            }
        }

        // Add IAmazonS3 when missing
        if (!_services.Any(s => s.ServiceType == typeof(IAmazonS3)))
            _services.AddAmazonS3FromConfiguration(configuration, configSectionName);

        // KeyStore is not registered here — call its own DI helpers, or register it separately.

        // Resolve or register the encryption service
        string? encryptionServiceKeyToUse = null;
        if (!string.IsNullOrWhiteSpace(_encryptionServiceKeyName))
            encryptionServiceKeyToUse = _encryptionServiceKeyName;
        else if (_encryptionServiceFactory != null) {
            if (!_services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(_keyName) && s.ServiceType == typeof(ITwoKeyEncryptionService)))
                _services.AddKeyedSingleton<ITwoKeyEncryptionService>(_keyName, (provider, _) => _encryptionServiceFactory(provider));

            encryptionServiceKeyToUse = _keyName;
        }
        else if (!string.IsNullOrWhiteSpace(_encryptionServiceConfigSection)) {
            throw new InvalidOperationException(
                "WithEncryptionServiceFromConfigSection is not yet supported by S3FileStorageServiceBuilder. Register the ITwoKeyEncryptionService separately and reference it via WithEncryptionServiceKey.");
        }

        // Resolve the metadata store key, or fail if only a config section was given
        string? metadataStoreKeyToUse = null;
        if (!string.IsNullOrWhiteSpace(_metadataStoreKeyName))
            metadataStoreKeyToUse = _metadataStoreKeyName;
        else if (!string.IsNullOrWhiteSpace(_metadataStoreConfigSection)) {
            throw new InvalidOperationException(
                "WithMetadataStoreFromConfigSection is not yet supported by S3FileStorageServiceBuilder. Register the IFileMetadataStore separately and reference it via WithMetadataStoreKey.");
        }

        // Scoped keyed S3 service so it matches the scoped metadata store
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
                ITwoKeyEncryptionService? encryptionService;
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
                return new(
                    options, metadataService, loggerFactory, compressionService, encryptionService, s3Client, metrics, operationContextAccessor, auditHandlers, contentPolicy);
            });

        _services.AddKeyedScoped<IFileStorageService>(
            _keyName,
            (provider, _) => provider.GetRequiredKeyedService<S3FileStorageService>(_keyName) ??
                throw new InvalidOperationException($"Keyed S3 file storage service '{_keyName}' was not found."));

        _services.TryAddInMemoryMultipartUploadSessionStoreIfMissing();

        // Multipart shares the same IAmazonS3 client as single-part storage; skip if already registered.
        if (!_services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(_keyName) && s.ServiceType == typeof(S3MultipartUploadService)))
            _services.AddKeyedS3MultipartUploadService(_keyName);

        return _services;
    }
}