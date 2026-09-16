using Lyo.Common.Core.Identifiers;

namespace Lyo.FileManager;

/// <summary>A saved remote API or a local disk store.</summary>
public sealed class FileManagerServer
{
    /// <summary>Stable id for the server list.</summary>
    public Guid Id { get; set; } = LyoGuid.CreateV7();

    /// <summary>Name shown in the server picker.</summary>
    public string Name { get; set; } = "";

    /// <summary>Remote HTTP API or an in-process store.</summary>
    public FileManagerServerKind Kind { get; set; }

    /// <summary>Remote API origin, for example <c>http://localhost:5000</c>.</summary>
    public string BaseUrl { get; set; } = "";

    /// <summary>File-storage route group. Starts as <c>FileStorage</c>.</summary>
    public string Route { get; set; } = "FileStorage";

    /// <summary>QueryProject route. Empty uses <c>{Route}/FileMetadata</c>.</summary>
    public string FileMetadataRoute { get; set; } = "";

    /// <summary>How <see cref="Secret" /> is sent. Ignored for a local server.</summary>
    public FileManagerAuthKind AuthKind { get; set; }

    /// <summary>Bearer token or API key. Null when the remote server is anonymous.</summary>
    public string? Secret { get; set; }

    /// <summary>Directory that holds local file bytes.</summary>
    public string RootDirectory { get; set; } = "";

    /// <summary>SQLite file or a Postgres connection string.</summary>
    public FileManagerMetadataKind MetadataKind { get; set; }

    /// <summary>SQLite connection string or Postgres connection string. Empty SQLite uses a file beside <see cref="RootDirectory" />.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>Optional passphrase. The same value recreates the local encryption key on the next launch.</summary>
    public string? Passphrase { get; set; }

    /// <summary>Local keys or AWS Secrets Manager. Used when no <see cref="KeyStoreId" /> is selected.</summary>
    public FileManagerKeyStoreKind KeyStoreKind { get; set; }

    /// <summary>Saved key store to open with this server. Null uses no catalog entry.</summary>
    public Guid? KeyStoreId { get; set; }

    /// <summary>AWS region, such as <c>us-east-1</c>. Used when <see cref="KeyStoreKind" /> is <see cref="FileManagerKeyStoreKind.Aws" />.</summary>
    public string? AwsRegion { get; set; }

    /// <summary>Secrets Manager name prefix. Empty uses <c>lyo/kek</c>.</summary>
    public string? AwsSecretPrefix { get; set; }

    /// <summary>AWS access key. Empty uses the default credential chain.</summary>
    public string? AwsAccessKeyId { get; set; }

    /// <summary>AWS secret key paired with <see cref="AwsAccessKeyId" />.</summary>
    public string? AwsSecretAccessKey { get; set; }

    /// <summary>Local keys recreated on the next connect. Ignored for AWS.</summary>
    public List<FileManagerStoredKey> Keys { get; set; } = [];

    /// <summary>S3 bucket. Required when <see cref="Kind" /> is <see cref="FileManagerServerKind.S3" />.</summary>
    public string? BucketName { get; set; }

    /// <summary>Azure Blob container. Required when <see cref="Kind" /> is <see cref="FileManagerServerKind.AzureBlob" />.</summary>
    public string? ContainerName { get; set; }

    /// <summary>Azure Storage connection string. Required for <see cref="FileManagerServerKind.AzureBlob" />.</summary>
    public string? AzureStorageConnectionString { get; set; }

    /// <summary>Optional S3 service URL for a compatible host.</summary>
    public string? ServiceUrl { get; set; }

    /// <summary>Optional object-key prefix for S3.</summary>
    public string? StorageKeyPrefix { get; set; }

    /// <summary>Optional AWS profile when static keys are omitted.</summary>
    public string? AwsProfile { get; set; }

    /// <summary>FTP or SFTP host.</summary>
    public string? Host { get; set; }

    /// <summary>FTP or SFTP port. Null keeps the protocol default.</summary>
    public int? Port { get; set; }

    /// <summary>FTP or SFTP username.</summary>
    public string? Username { get; set; }

    /// <summary>FTP or SFTP password.</summary>
    public string? Password { get; set; }

    /// <summary>Optional SFTP private-key path.</summary>
    public string? PrivateKeyPath { get; set; }

    /// <summary>SFTP host-key fingerprints, separated by commas or spaces.</summary>
    public string? HostKeyFingerprints { get; set; }

    /// <summary>True when this server can encrypt: a passphrase, a saved local key, or an AWS key store.</summary>
    public bool HasKeyStore()
    {
        if (!string.IsNullOrWhiteSpace(Passphrase))
            return true;
        if (Keys.Any(key => !string.IsNullOrWhiteSpace(key.Id) && !string.IsNullOrWhiteSpace(key.Secret)))
            return true;
        return KeyStoreKind == FileManagerKeyStoreKind.Aws;
    }

    /// <summary>FileMetadata route with the default filled in.</summary>
    public string MetadataRouteOrDefault()
        => string.IsNullOrWhiteSpace(FileMetadataRoute) ? Route.Trim().Trim('/') + "/FileMetadata" : FileMetadataRoute.Trim().Trim('/');

    /// <inheritdoc />
    public override string ToString() => string.IsNullOrWhiteSpace(Name) ? Kind.ToString() : Name;
}

/// <summary>Where a server's bytes and metadata live.</summary>
public enum FileManagerServerKind
{
    /// <summary>HTTP FileStorage API.</summary>
    Remote,

    /// <summary>In-process disk store.</summary>
    Local,

    /// <summary>In-process S3 or S3-compatible bucket.</summary>
    S3,

    /// <summary>In-process Azure Blob container.</summary>
    AzureBlob,

    /// <summary>In-process FTP store.</summary>
    Ftp,

    /// <summary>In-process SFTP store.</summary>
    Sftp
}

/// <summary>Credential sent on a remote server.</summary>
public enum FileManagerAuthKind
{
    /// <summary>No credential header.</summary>
    None,

    /// <summary><c>Authorization: Bearer</c>.</summary>
    Bearer,

    /// <summary><c>X-Api-Key</c>.</summary>
    ApiKey
}

/// <summary>Where encryption keys for a local server come from.</summary>
public enum FileManagerKeyStoreKind
{
    /// <summary>Keys kept in this settings file and a passphrase.</summary>
    Local,

    /// <summary>AWS Secrets Manager.</summary>
    Aws
}

/// <summary>A named local or AWS key store. Several can be saved, and a server connects with one of them.</summary>
public sealed class FileManagerKeyStore
{
    /// <summary>Stable id stored on <see cref="FileManagerServer.KeyStoreId" />.</summary>
    public Guid Id { get; set; } = LyoGuid.CreateV7();

    /// <summary>Name shown in the key store picker.</summary>
    public string Name { get; set; } = "";

    /// <summary>Local keys or AWS Secrets Manager.</summary>
    public FileManagerKeyStoreKind Kind { get; set; }

    /// <summary>Optional passphrase. The same value recreates <see cref="FileManagerLimits.LocalKeyId" /> on the next connect.</summary>
    public string? Passphrase { get; set; }

    /// <summary>Local keys recreated on the next connect. Ignored for AWS.</summary>
    public List<FileManagerStoredKey> Keys { get; set; } = [];

    /// <summary>AWS region, such as <c>us-east-1</c>. Used when <see cref="Kind" /> is <see cref="FileManagerKeyStoreKind.Aws" />.</summary>
    public string? AwsRegion { get; set; }

    /// <summary>Secrets Manager name prefix. Empty uses <c>lyo/kek</c>.</summary>
    public string? AwsSecretPrefix { get; set; }

    /// <summary>AWS access key. Empty uses the default credential chain.</summary>
    public string? AwsAccessKeyId { get; set; }

    /// <summary>AWS secret key paired with <see cref="AwsAccessKeyId" />.</summary>
    public string? AwsSecretAccessKey { get; set; }

    /// <summary>Optional AWS profile. The window copies its region and keys into this store.</summary>
    public string? AwsProfile { get; set; }

    /// <summary>True when this store can encrypt: a passphrase, a saved local key, or AWS.</summary>
    public bool HasMaterial()
    {
        if (Kind == FileManagerKeyStoreKind.Aws)
            return true;
        if (!string.IsNullOrWhiteSpace(Passphrase))
            return true;
        return Keys?.Any(key => !string.IsNullOrWhiteSpace(key.Id) && (!string.IsNullOrWhiteSpace(key.Secret) || !string.IsNullOrWhiteSpace(key.RawKey))) == true;
    }

    /// <inheritdoc />
    public override string ToString() => string.IsNullOrWhiteSpace(Name) ? "Key store" : Name;
}

/// <summary>One local encryption key saved with a key store.</summary>
public sealed class FileManagerStoredKey
{
    /// <summary>Key id used on upload.</summary>
    public string Id { get; set; } = "";

    /// <summary>Key version. Empty uses <c>1</c>.</summary>
    public string Version { get; set; } = "1";

    /// <summary>Passphrase. Empty when <see cref="RawKey" /> holds the key bytes.</summary>
    public string Secret { get; set; } = "";

    /// <summary>Base64 key bytes from a key file. When set, this is the key itself and <see cref="Secret" /> is not derived.</summary>
    public string? RawKey { get; set; }

    /// <summary>True when this version is the current one for <see cref="Id" />.</summary>
    public bool Current { get; set; }

    /// <summary>Metadata from the key file, including the derivation salt.</summary>
    public Lyo.KeyStore.KeyMetadata? Metadata { get; set; }
}

/// <summary>Metadata database for a local server.</summary>
public enum FileManagerMetadataKind
{
    /// <summary>SQLite file.</summary>
    Sqlite,

    /// <summary>Postgres connection string.</summary>
    Postgres
}
