using Lyo.Compression;
using Lyo.Encryption.TwoKey;
using Lyo.Exceptions;
using Lyo.FileMetadataStore;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FileStorage;

/// <summary>
/// Shared steps for one named store: options, metadata, encryption, and compression. <see cref="FromConfiguration" /> binds a section. <see cref="Configure" /> runs after
/// that and edits the same options, so inline values win. Encryption and compression services are selected by name. They are not built here.
/// </summary>
public abstract class FileStorageServiceBuilder<TOptions, TBuilder>
    where TOptions : FileStorageServiceBaseOptions, new() where TBuilder : FileStorageServiceBuilder<TOptions, TBuilder>
{
    /// <summary>Options bound from configuration and then edited inline.</summary>
    protected TOptions Options { get; } = new();

    internal string? MetadataKey { get; private set; }

    internal Func<IServiceProvider, IFileMetadataStore>? MetadataFactory { get; private set; }

    internal string? EncryptionKey { get; private set; }

    internal Func<IServiceProvider, ITwoKeyEncryptionService>? EncryptionFactory { get; private set; }

    internal string? CompressionKey { get; private set; }

    internal Func<IServiceProvider, ICompressionService>? CompressionFactory { get; private set; }

    internal bool Archive { get; private set; }

    private TBuilder Self => (TBuilder)this;

    /// <summary>Edits the options object. Call this after <see cref="FromConfiguration" /> to override individual fields.</summary>
    public TBuilder Configure(Action<TOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        configure(Options);
        return Self;
    }

    /// <summary>Binds <paramref name="sectionName" /> onto the options, then reads metadata, encryption, and compression keys from that section.</summary>
    public TBuilder FromConfiguration(IConfiguration configuration, string sectionName)
    {
        ArgumentHelpers.ThrowIfNull(configuration);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sectionName);
        return ApplySection(configuration.GetSection(sectionName));
    }

    /// <summary>Resolves <see cref="IFileMetadataStore" /> from a keyed registration.</summary>
    public TBuilder UseMetadata(string key)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        MetadataKey = key.Trim();
        MetadataFactory = null;
        return Self;
    }

    /// <summary>Builds the metadata store from <paramref name="metadata" />.</summary>
    public TBuilder WithMetadata(Func<IServiceProvider, IFileMetadataStore> metadata)
    {
        ArgumentHelpers.ThrowIfNull(metadata);
        MetadataFactory = metadata;
        MetadataKey = null;
        return Self;
    }

    /// <summary>Resolves <see cref="ITwoKeyEncryptionService" /> from a keyed registration. The key store stays on that service.</summary>
    public TBuilder UseEncryption(string key)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        EncryptionKey = key.Trim();
        EncryptionFactory = null;
        return Self;
    }

    /// <summary>Builds the encryption service from <paramref name="encryption" />.</summary>
    public TBuilder WithEncryption(Func<IServiceProvider, ITwoKeyEncryptionService> encryption)
    {
        ArgumentHelpers.ThrowIfNull(encryption);
        EncryptionFactory = encryption;
        EncryptionKey = null;
        return Self;
    }

    /// <summary>Resolves <see cref="ICompressionService" /> from a keyed registration. Omit this to use the unkeyed compression service.</summary>
    public TBuilder UseCompression(string key)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        CompressionKey = key.Trim();
        CompressionFactory = null;
        return Self;
    }

    /// <summary>Builds the compression service from <paramref name="compression" />.</summary>
    public TBuilder WithCompression(Func<IServiceProvider, ICompressionService> compression)
    {
        ArgumentHelpers.ThrowIfNull(compression);
        CompressionFactory = compression;
        CompressionKey = null;
        return Self;
    }

    /// <summary>Registers keyed services for this store. Provider packages override this for multipart. The factory calls it after the store is recorded.</summary>
    public virtual void RegisterFactoryServices(IServiceCollection services, string storeName) { }

    /// <summary>Registers a keyed archive service for this store when the factory records it. Archive options stay the unkeyed registration.</summary>
    public TBuilder WithArchive()
    {
        Archive = true;
        return Self;
    }

    /// <summary>
    /// Binds <paramref name="section" /> and reads reserved metadata, encryption, and compression keys. Provider packages call this from
    /// <see cref="IFileStorageStoreProvider.Create" />.
    /// </summary>
    public TBuilder ApplySection(IConfiguration section)
    {
        ArgumentHelpers.ThrowIfNull(section);
        ReadReserved(section);
        Bind(section);
        return Self;
    }

    /// <summary>Returns the delegate that constructs this store. Call <see cref="EnsureValid" /> first.</summary>
    public Func<IServiceProvider, IFileStorageService> ToDelegate(string storeName) => services => Create(services, storeName);

    /// <summary>Copies provider settings from <paramref name="section" /> onto <see cref="Options" />.</summary>
    protected abstract void Bind(IConfiguration section);

    /// <summary>Constructs the store. Omitted encryption passes null. Omitted compression uses the unkeyed <see cref="ICompressionService" />.</summary>
    protected abstract IFileStorageService Create(IServiceProvider services, string storeName);

    /// <summary>Validates <see cref="Options" /> before the store is recorded.</summary>
    protected abstract void Validate();

    /// <summary>Validates <see cref="Options" /> before the store is recorded.</summary>
    public void EnsureValid() => Validate();

    private void ReadReserved(IConfiguration section)
    {
        var metadata = section[FileStorageFactoryKeys.MetadataStoreKey];
        if (!string.IsNullOrWhiteSpace(metadata))
            UseMetadata(metadata.Trim());

        var encryption = section[FileStorageFactoryKeys.EncryptionServiceKey];
        if (!string.IsNullOrWhiteSpace(encryption))
            UseEncryption(encryption.Trim());

        var compression = section[FileStorageFactoryKeys.CompressionServiceKey];
        if (!string.IsNullOrWhiteSpace(compression))
            UseCompression(compression.Trim());
    }

    /// <summary>Resolves metadata. A missing name uses the unkeyed store and throws when that store is absent.</summary>
    protected IFileMetadataStore ResolveRequiredMetadata(IServiceProvider services, string storeName)
    {
        var metadata = ResolveMetadata(services, storeName);
        if (metadata != null)
            return metadata;

        return services.GetService<IFileMetadataStore>() ?? throw new InvalidOperationException($"File storage '{storeName}' metadata store is not registered.");
    }

    /// <summary>Resolves the metadata store. Null lets disk open its local catalog.</summary>
    protected IFileMetadataStore? ResolveMetadata(IServiceProvider services, string storeName)
    {
        if (MetadataFactory != null)
            return MetadataFactory(services);

        if (string.IsNullOrWhiteSpace(MetadataKey))
            return null;

        return services.GetKeyedService<IFileMetadataStore>(MetadataKey) ?? throw Missing(storeName, "metadata store", MetadataKey);
    }

    /// <summary>Resolves encryption. Null when the store omitted it, including when an unkeyed encryption service is registered.</summary>
    protected ITwoKeyEncryptionService? ResolveEncryption(IServiceProvider services, string storeName)
    {
        if (EncryptionFactory != null)
            return EncryptionFactory(services);

        if (string.IsNullOrWhiteSpace(EncryptionKey))
            return null;

        return services.GetKeyedService<ITwoKeyEncryptionService>(EncryptionKey) ?? throw Missing(storeName, "encryption service", EncryptionKey);
    }

    /// <summary>Resolves compression. A missing name uses the unkeyed service, which may itself be null.</summary>
    protected ICompressionService? ResolveCompression(IServiceProvider services, string storeName)
    {
        if (CompressionFactory != null)
            return CompressionFactory(services);

        if (string.IsNullOrWhiteSpace(CompressionKey))
            return services.GetService<ICompressionService>();

        return services.GetKeyedService<ICompressionService>(CompressionKey) ?? throw Missing(storeName, "compression service", CompressionKey);
    }

    private static InvalidOperationException Missing(string storeName, string kind, string key) => new($"File storage '{storeName}' {kind} '{key}' is not registered.");
}