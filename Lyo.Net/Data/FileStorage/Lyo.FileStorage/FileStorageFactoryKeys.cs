namespace Lyo.FileStorage;

/// <summary>Configuration keys the factory reads itself. They are not bound onto a backend options object.</summary>
public static class FileStorageFactoryKeys
{
    /// <summary>Keyed <c>IFileMetadataStore</c> name.</summary>
    public const string MetadataStoreKey = "MetadataStoreKey";

    /// <summary>Keyed <c>ITwoKeyEncryptionService</c> name.</summary>
    public const string EncryptionServiceKey = "EncryptionServiceKey";

    /// <summary>Keyed <c>ICompressionService</c> name.</summary>
    public const string CompressionServiceKey = "CompressionServiceKey";

    /// <summary>Section child that holds named stores.</summary>
    public const string Stores = "Stores";

    /// <summary>Optional sibling of <see cref="Stores" />. The bare HTTP route serves this store.</summary>
    public const string Default = "Default";

    /// <summary>Desktop catalog connection. Not the Azure storage connection string.</summary>
    public const string CatalogConnectionString = "CatalogConnectionString";

    /// <summary>Desktop catalog kind: <c>Sqlite</c> or <c>Postgres</c>.</summary>
    public const string MetadataKind = "MetadataKind";
}
