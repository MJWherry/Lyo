using Amazon;
using Amazon.SecretsManager;
using Lyo.Compression;
using Lyo.Encryption.TwoKey;
using Lyo.Exceptions;
using Lyo.FileMetadataStore;
using Lyo.FileMetadataStore.DownloadAccess;
using Lyo.FileMetadataStore.Models;
using Lyo.FileMetadataStore.Postgres;
using Lyo.FileMetadataStore.Postgres.Database;
using Lyo.FileMetadataStore.Sqlite;
using Lyo.FileMetadataStore.Sqlite.Database;
using Lyo.FileStorage;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.AzureBlob;
using Lyo.FileStorage.Ftp;
using Lyo.FileStorage.Models;
using Lyo.FileStorage.Multipart;
using Lyo.FileStorage.S3;
using Lyo.FileStorage.Sftp;
using Lyo.IO.Temp;
using Lyo.Ftp.Client;
using Lyo.KeyStore;
using Lyo.KeyStore.Aws;
using Lyo.Sftp.Client;
using Lyo.Lock;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ApiPresence = Lyo.Api.FileStorage.Models.FileStoragePresence;

namespace Lyo.FileManager;

/// <summary>Which metadata registration a local server will use. Building this does not open a database.</summary>
public sealed record FileStorageLocalPlan(FileManagerMetadataKind MetadataKind, string ConnectionString, string RootDirectory);

/// <summary>In-process FileStorage on a directory plus SQLite or Postgres.</summary>
public sealed class FileStorageLocalHost : IFileManagerApi, IFileManagerTransfer
{
    private const string KeyService = "file-manager-keys";

    private readonly FileManagerServer _server;
    private readonly ServiceProvider? _provider;
    private readonly bool _ownsProvider;
    private readonly IServiceScope _scope;
    private readonly IFileMetadataStore _store;
    private readonly IFileStorageService _storage;
    private readonly IMultipartUploadService? _multipart;
    private readonly IFileStorageArchiveService _archive;
    private readonly IFileDownloadAccessService _access;
    private readonly IKeyStore? _keys;
    private readonly IKeyStore _endpointKeys;

    private FileStorageLocalHost(
        FileManagerServer server,
        ServiceProvider? provider,
        IServiceScope scope,
        IFileMetadataStore store,
        IFileStorageService storage,
        IMultipartUploadService? multipart,
        IFileStorageArchiveService archive,
        IFileDownloadAccessService access,
        IKeyStore? keys,
        bool ownsProvider = true)
    {
        _server = server;
        _provider = provider;
        _ownsProvider = ownsProvider;
        _scope = scope;
        _store = store;
        _storage = storage;
        _multipart = multipart;
        _archive = archive;
        _access = access;
        _keys = keys;
        _endpointKeys = keys ?? new LocalKeyStore();
    }

    /// <summary>Storage, catalog, and keys for <see cref="FileStorageTransfer" />. An empty key store is used when this server has no passphrase.</summary>
    public FileStorageTransferEndpoint Endpoint => new(_storage, _store, _endpointKeys);

    /// <summary>Key store opened with this host. Null when the server has no keys.</summary>
    internal IKeyStore? KeyStore => _keys;

    /// <summary>Connection string and provider for <paramref name="server" />. Does not connect.</summary>
    public static FileStorageLocalPlan Describe(FileManagerServer server)
    {
        ArgumentHelpers.ThrowIfNull(server);
        if (server.Kind == FileManagerServerKind.Remote)
            throw new InvalidOperationException("Describe is for an in-process store.");
        if (server.Kind != FileManagerServerKind.Local)
            return new(server.MetadataKind, CatalogConnection(server), server.RootDirectory ?? "");
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(server.RootDirectory);
        var connection = server.MetadataKind == FileManagerMetadataKind.Postgres
            ? server.ConnectionString
            : string.IsNullOrWhiteSpace(server.ConnectionString)
                ? "Data Source=" + FileManagerPaths.DefaultSqliteFile(server.RootDirectory)
                : server.ConnectionString;
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connection);
        if (server.MetadataKind == FileManagerMetadataKind.Sqlite) {
            var inside = FileManagerPaths.RejectDatabaseInsideRoot(server.RootDirectory, connection);
            if (inside != null)
                throw new InvalidOperationException(inside);
        }
        return new(server.MetadataKind, connection, server.RootDirectory);
    }

    private static string CatalogConnection(FileManagerServer server)
    {
        if (server.MetadataKind == FileManagerMetadataKind.Postgres) {
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(server.ConnectionString);
            return server.ConnectionString;
        }

        if (string.IsNullOrWhiteSpace(server.ConnectionString))
            throw new InvalidOperationException("Catalog database is required.");
        var text = server.ConnectionString.Trim();
        return text.Contains('=') ? text : "Data Source=" + text;
    }

    private static IFileStorageService CreateStorage(
        FileManagerServer server,
        FileStorageLocalPlan plan,
        IFileMetadataStore store,
        ILoggerFactory? logs,
        CompressionService compression,
        ITwoKeyEncryptionService? encryption)
        => server.Kind switch {
            FileManagerServerKind.S3 => CreateS3(server, store, logs, compression, encryption),
            FileManagerServerKind.AzureBlob => CreateAzure(server, store, logs, compression, encryption),
            FileManagerServerKind.Ftp => CreateFtp(server, store, logs, compression, encryption),
            FileManagerServerKind.Sftp => CreateSftp(server, store, logs, compression, encryption),
            _ => new LocalFileStorageService(
                new DiskFileStorageOptions { RootDirectoryPath = plan.RootDirectory, AllowFileUriPresignedUrls = true },
                logs, compression, encryption, store)
        };

    private static S3FileStorageService CreateS3(
        FileManagerServer server, IFileMetadataStore store, ILoggerFactory? logs, CompressionService compression, ITwoKeyEncryptionService? encryption)
    {
        var options = new S3FileStorageOptions {
            BucketName = server.BucketName ?? "",
            Region = EmptyToNull(server.AwsRegion),
            AccessKeyId = EmptyToNull(server.AwsAccessKeyId),
            SecretAccessKey = server.AwsSecretAccessKey,
            Profile = EmptyToNull(server.AwsProfile),
            ServiceUrl = EmptyToNull(server.ServiceUrl),
            KeyPrefix = EmptyToNull(server.StorageKeyPrefix)
        };
        options.Validate();
        return new(options, store, logs, compression, encryption);
    }

    private static AzureBlobFileStorageService CreateAzure(
        FileManagerServer server, IFileMetadataStore store, ILoggerFactory? logs, CompressionService compression, ITwoKeyEncryptionService? encryption)
    {
        var options = new AzureBlobFileStorageOptions {
            ConnectionString = server.AzureStorageConnectionString ?? "",
            ContainerName = server.ContainerName ?? ""
        };
        options.Validate();
        return new(options, store, logs, compression, encryption);
    }

    private static FtpFileStorageService CreateFtp(
        FileManagerServer server, IFileMetadataStore store, ILoggerFactory? logs, CompressionService compression, ITwoKeyEncryptionService? encryption)
    {
        var ftp = new FtpClientOptions {
            Host = server.Host ?? "",
            Username = server.Username ?? "",
            Password = server.Password,
            RootRemoteDirectory = string.IsNullOrWhiteSpace(server.RootDirectory) ? "/" : server.RootDirectory
        };
        if (server.Port is > 0)
            ftp.Port = server.Port.Value;
        var options = new FtpFileStorageOptions { Ftp = ftp };
        options.Validate();
        return new(options, store, new FtpClient(ftp, logs), logs, compression, encryption);
    }

    private static SftpFileStorageService CreateSftp(
        FileManagerServer server, IFileMetadataStore store, ILoggerFactory? logs, CompressionService compression, ITwoKeyEncryptionService? encryption)
    {
        var sftp = new SftpClientOptions {
            Host = server.Host ?? "",
            Username = server.Username ?? "",
            Password = server.Password,
            PrivateKeyPath = EmptyToNull(server.PrivateKeyPath),
            RootRemoteDirectory = string.IsNullOrWhiteSpace(server.RootDirectory) ? "/" : server.RootDirectory,
            AllowedHostKeyFingerprints = SplitFingerprints(server.HostKeyFingerprints)
        };
        if (server.Port is > 0)
            sftp.Port = server.Port.Value;
        var options = new SftpFileStorageOptions { Sftp = sftp };
        options.Validate();
        return new(options, store, new SftpClient(sftp, logs), logs, compression, encryption);
    }

    private static List<string> SplitFingerprints(string? text)
        => string.IsNullOrWhiteSpace(text)
            ? []
            : text.Split([',', ' ', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Registers the metadata store and nothing else. Does not open a connection.</summary>
    public static ServiceCollection CreateServices(FileManagerServer server)
    {
        var plan = Describe(server);
        var services = new ServiceCollection();
        if (plan.MetadataKind == FileManagerMetadataKind.Postgres)
            services.AddPostgresFileMetadataStore(plan.ConnectionString);
        else
            services.AddSqliteFileMetadataStore(plan.ConnectionString);
        return services;
    }

    /// <summary>Migrates the metadata database and opens the store. Keys come from the passphrase and saved keys on <paramref name="server" />.</summary>
    public static FileStorageLocalHost Open(FileManagerServer server, ILoggerFactory? logs = null)
        => Open(server, logs, CreateKeyStore(server));

    /// <summary>
    /// Migrates the metadata database and opens the store with <paramref name="keyStore" />.
    /// Null means no keys, even when <paramref name="server" /> still has a passphrase.
    /// </summary>
    public static FileStorageLocalHost Open(FileManagerServer server, FileManagerKeyStore? keyStore, ILoggerFactory? logs = null)
        => Open(server, logs, keyStore == null ? null : CreateKeyStore(keyStore));

    private static FileStorageLocalHost Open(FileManagerServer server, ILoggerFactory? logs, IKeyStore? keys)
    {
        var plan = Describe(server);
        if (server.Kind == FileManagerServerKind.Local)
            Directory.CreateDirectory(plan.RootDirectory);
        var services = new ServiceCollection();
        AddCatalog(services, plan.MetadataKind, plan.ConnectionString);
        services.AddLocalLock();
        services.AddSingleton<ICompressionAlgorithmSelector, UploadCompressionSelector>();
        services.AddCompressionService();
        services.AddIOTempService();
        ITwoKeyEncryptionService? encryption = null;
        if (keys != null) {
            services.AddKeyedSingleton<IKeyStore>(KeyService, keys);
            encryption = FileManagerEncryption.Create(keys);
            services.AddKeyedSingleton<ITwoKeyEncryptionService>(KeyService, encryption);
        }

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        try {
            Migrate(scope.ServiceProvider, plan.MetadataKind);
            IFileMetadataStore store = CatalogMetadata(scope.ServiceProvider, plan.MetadataKind);
            var compression = scope.ServiceProvider.GetRequiredService<CompressionService>();
            var storage = CreateStorage(server, plan, store, logs, compression, encryption);
            IMultipartUploadService? multipart = null;
            if (storage is LocalFileStorageService local) {
                var options = new DiskFileStorageOptions { RootDirectoryPath = plan.RootDirectory, AllowFileUriPresignedUrls = true };
                multipart = new LocalMultipartUploadService(local, scope.ServiceProvider.GetRequiredService<IMultipartUploadSessionStore>(), options);
            }
            var archive = new FileStorageArchiveService(storage, scope.ServiceProvider.GetRequiredService<IIOTempService>(), new FileStorageArchiveOptions());
            var access = scope.ServiceProvider.GetRequiredService<IFileDownloadAccessService>();
            return new(server, provider, scope, store, storage, multipart, archive, access, keys);
        }
        catch {
            scope.Dispose();
            provider.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<FileManagerHealth> CheckHealthAsync(CancellationToken ct)
    {
        var health = await _storage.CheckHealthAsync(ct).ConfigureAwait(false);
        return new(health.IsHealthy, health.Message);
    }

    /// <inheritdoc />
    public async Task<Lyo.Api.FileStorage.Models.FileStorageCapabilitiesResponse> GetCapabilitiesAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var features = ((FileStorageServiceBase)_storage).GetCapabilities();
        var tagTypes = _storage is IFileStorageTags tags
            ? (await tags.GetTagTypesAsync(ct).ConfigureAwait(false)).Select(ToTagType).ToArray()
            : [];
        return new(
            features.PresignedRead,
            features.DirectUpload,
            true,
            _multipart is not null && _multipart is not LocalMultipartUploadService,
            features.Copy,
            features.Move,
            features.Physical,
            _keys != null,
            true,
            true,
            tagTypes,
            FileTagValueKinds.All.Select(ToKind).ToArray());
    }

    private static Lyo.Api.FileStorage.Models.FileStorageTagTypeResponse ToTagType(FileTagTypeInfo type)
        => new(type.TagType, type.Automatic, type.Sort.ToString(), type.Grains.Select(static grain => grain.ToString()).ToArray(), FileTagValueKinds.Name(type.ValueKind));

    private static Lyo.Api.FileStorage.Models.FileStorageTagValueKindResponse ToKind(FileTagValueKind kind)
        => new(FileTagValueKinds.Name(kind), FileTagValueKinds.SortFor(kind).ToString(), FileTagValueKinds.GrainsFor(kind).Select(static grain => grain.ToString()).ToArray());

    /// <inheritdoc />
    public async Task<FileManagerListing> ListAsync(string? pathPrefix, CancellationToken ct)
    {
        var (entries, truncated) = await FileStorageFolderList.ListAsync(_store, _storage, pathPrefix, ct: ct).ConfigureAwait(false);
        return new(entries.Select(ToEntry).ToArray(), truncated);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileManagerQueryRow>> QueryAsync(string? nameContains, string? pathPrefix, CancellationToken ct)
    {
        var rows = await _store.ListByPathPrefixAsync(pathPrefix, true, 10_000, ct).ConfigureAwait(false);
        var name = nameContains?.Trim();
        return rows
            .Where(row => string.IsNullOrEmpty(name) || (row.OriginalFileName?.Contains(name, StringComparison.OrdinalIgnoreCase) ?? false))
            .Select(row => new FileManagerQueryRow(row.Id, row.OriginalFileName, row.PathPrefix, row.OriginalFileSize))
            .ToArray();
    }

    /// <inheritdoc />
    public async Task UploadAsync(string filePath, string? pathPrefix, FileManagerUploadOptions options, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentHelpers.ThrowIfNull(options);
        if (options.Encrypt && string.IsNullOrWhiteSpace(options.KeyId) && _keys == null)
            throw new InvalidOperationException("No key is configured.");
        var length = new FileInfo(filePath).Length;
        var threshold = options.MultipartThresholdBytes ?? FileManagerLimits.MultipartThresholdBytes;
        using (FileManagerUploadScope.Push(options)) {
            if (_multipart != null && length > threshold) {
                await UploadMultipartAsync(filePath, pathPrefix, options, length, ct).ConfigureAwait(false);
                return;
            }

            await _storage.SaveFileAsync(
                    filePath,
                    Path.GetFileName(filePath),
                    options.Compress,
                    options.Encrypt,
                    options.KeyId,
                    pathPrefix,
                    contentType: options.ContentType,
                    charset: options.Charset,
                    ct: ct)
                .ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task DownloadAsync(Guid fileId, string destinationPath, CancellationToken ct)
    {
        var stream = await _storage.GetFileStreamAsync(fileId, ct: ct).ConfigureAwait(false) ?? throw new FileNotFoundException("File was not found.", fileId.ToString());
        await using (stream)
            await CopyAsync(stream, destinationPath, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DownloadArchiveAsync(IReadOnlyList<Guid> fileIds, string destinationPath, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(fileIds);
        var archive = await _archive.CreateArchiveAsync(fileIds.Select(id => new FileStorageArchiveEntry(id)).ToArray(), Path.GetFileName(destinationPath), ct)
            .ConfigureAwait(false);
        await using var stream = archive.Stream;
        await CopyAsync(stream, destinationPath, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<FileStoreResult> GetMetadataAsync(Guid fileId, bool includeDeleted, CancellationToken ct) => _storage.GetMetadataAsync(fileId, ct);

    /// <inheritdoc />
    public Task RenameAsync(Guid fileId, string originalFileName, CancellationToken ct)
        => _storage.RenameFileAsync(fileId, new RenameFileRequest { OriginalFileName = originalFileName }, ct);

    /// <inheritdoc />
    public Task MoveAsync(Guid fileId, string? pathPrefix, CancellationToken ct)
        => _storage.MoveFileAsync(fileId, new MoveFileRequest { PathPrefix = pathPrefix }, ct);

    /// <inheritdoc />
    public Task CopyAsync(Guid fileId, string? pathPrefix, CancellationToken ct)
        => _storage.CopyFileAsync(fileId, new CopyFileRequest { PathPrefix = pathPrefix }, ct);

    /// <inheritdoc />
    public Task DeleteAsync(Guid fileId, CancellationToken ct) => _storage.DeleteFileAsync(fileId, ct: ct);

    /// <inheritdoc />
    public async Task<string> CreateAccessLinkAsync(Guid fileId, DateTime? expiresAtUtc, int? maxDownloads, CancellationToken ct)
    {
        var created = await _access.CreateLinkAsync(new(fileId, ExpiresAtUtc: expiresAtUtc, MaxDownloads: maxDownloads), ct).ConfigureAwait(false);
        return "files/access/" + created.Token + "/download";
    }

    /// <inheritdoc />
    public Task<string> PresignedReadAsync(Guid fileId, CancellationToken ct) => _storage.GetPreSignedReadUrlAsync(fileId, ct: ct);

    /// <inheritdoc />
    public async Task DownloadPhysicalAsync(string physicalKey, string destinationPath, CancellationToken ct)
    {
        var (stream, _) = await FileStoragePhysicalRead.OpenAsync(_storage, physicalKey, ct).ConfigureAwait(false);
        await using (stream)
            await CopyAsync(stream, destinationPath, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task RenamePhysicalAsync(string physicalKey, string newFileName, CancellationToken ct)
        => FileStoragePhysicalMutate.RenameAsync(_storage, physicalKey, newFileName, ct);

    /// <inheritdoc />
    public Task MovePhysicalAsync(string physicalKey, string? destinationPrefix, CancellationToken ct)
        => FileStoragePhysicalMutate.MoveToPrefixAsync(_storage, physicalKey, destinationPrefix, ct);

    /// <inheritdoc />
    public Task CopyPhysicalAsync(string physicalKey, string? destinationPrefix, CancellationToken ct)
        => FileStoragePhysicalMutate.CopyToPrefixAsync(_storage, physicalKey, destinationPrefix, ct);

    /// <inheritdoc />
    public Task DeletePhysicalAsync(string physicalKey, CancellationToken ct) => FileStoragePhysicalMutate.DeleteAsync(_storage, physicalKey, ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ListKeyIdsAsync(CancellationToken ct)
    {
        if (_keys is IKeyInventoryStore inventory)
            return await inventory.GetAvailableKeyIdsAsync(ct).ConfigureAwait(false);
        return [];
    }

    /// <inheritdoc />
    public Task<DekMigrationResult> RotateAsync(IReadOnlyList<Guid> fileIds, string? targetKeyId, string? targetKeyVersion, CancellationToken ct)
    {
        if (_keys == null)
            throw new InvalidOperationException("No key is configured.");
        return _storage.RotateDeksAsync(fileIds, targetKeyId, targetKeyVersion, ct: ct);
    }

    /// <inheritdoc />
    public Task<DekMigrationResult> MigrateAsync(
        string sourceKeyId,
        string? sourceKeyVersion,
        string? targetKeyId,
        string? targetKeyVersion,
        CancellationToken ct)
    {
        if (_keys == null)
            throw new InvalidOperationException("No key is configured.");
        return _storage.MigrateDeksAsync(sourceKeyId, sourceKeyVersion, targetKeyId, targetKeyVersion, ct: ct);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsProvider) {
            if (_storage is IDisposable disposable)
                disposable.Dispose();
            if (_store is IDisposable store)
                store.Dispose();
        }

        _scope.Dispose();
        if (_ownsProvider)
            _provider?.Dispose();
    }

    /// <summary>Opens a store the factory already created. Disposing the host disposes <paramref name="scope" /> and leaves the root provider running.</summary>
    internal static FileStorageLocalHost FromFactory(
        FileManagerServer server,
        IServiceScope scope,
        IFileStorageService storage,
        IFileDownloadAccessService access,
        IMultipartUploadSessionStore sessions,
        IKeyStore? keys)
    {
        IMultipartUploadService? multipart = scope.ServiceProvider.GetKeyedService<IMultipartUploadService>(server.Name);
        if (multipart == null && storage is LocalFileStorageService local && !string.IsNullOrWhiteSpace(server.RootDirectory)) {
            multipart = new LocalMultipartUploadService(
                local, sessions, new DiskFileStorageOptions { RootDirectoryPath = server.RootDirectory, AllowFileUriPresignedUrls = true });
        }

        var archive = new FileStorageArchiveService(storage, scope.ServiceProvider.GetRequiredService<IIOTempService>(), new FileStorageArchiveOptions());
        if (storage is not FileStorageServiceBase backed)
            throw new InvalidOperationException($"File storage '{server.Name}' has no catalog.");
        return new(server, null, scope, backed.MetadataStore, storage, multipart, archive, access, keys, ownsProvider: false);
    }

    /// <summary>Registers the catalog, download access, and multipart sessions a saved server uses when it opens.</summary>
    internal static void AddCatalog(IServiceCollection services, FileManagerMetadataKind kind, string connection)
    {
        if (kind == FileManagerMetadataKind.Postgres)
            services.AddPostgresFileMetadataStore(connection);
        else
            services.AddSqliteFileMetadataStore(connection);

        if (kind == FileManagerMetadataKind.Sqlite) {
            services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<SqliteFileMetadataStoreDbContext>>().CreateDbContext());
            services.AddSqliteFileDownloadAccessService();
            services.AddSqliteMultipartUploadSessionStore();
        }
        else {
            services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<FileMetadataStoreDbContext>>().CreateDbContext());
            services.AddPostgresFileDownloadAccessService();
            services.AddPostgresMultipartUploadSessionStore();
        }
    }

    /// <summary>The catalog store resolved after <see cref="Migrate" />.</summary>
    internal static IFileMetadataStore CatalogMetadata(IServiceProvider services, FileManagerMetadataKind kind)
        => kind == FileManagerMetadataKind.Sqlite
            ? services.GetRequiredService<SqliteFileMetadataStore>()
            : services.GetRequiredService<PostgresFileMetadataStore>();

    private async Task UploadMultipartAsync(string filePath, string? pathPrefix, FileManagerUploadOptions options, long length, CancellationToken ct)
    {
        var multipart = _multipart ?? throw new InvalidOperationException("This store does not use multipart.");
        var begin = await multipart.BeginAsync(
                new MultipartBeginRequest {
                    DeclaredContentLength = length,
                    OriginalFileName = Path.GetFileName(filePath),
                    Compress = options.Compress,
                    Encrypt = options.Encrypt,
                    KeyId = options.KeyId,
                    PathPrefix = pathPrefix,
                    ContentType = options.ContentType,
                    Charset = options.Charset
                }, ct)
            .ConfigureAwait(false);
        try {
            await using (var file = File.OpenRead(filePath))
                await multipart.UploadPartAsync(begin.SessionId, 1, file, ct).ConfigureAwait(false);
            var completed = new CompleteMultipartUploadRequest {
                SessionId = begin.SessionId,
                Parts = [new CompletedPart { PartNumber = 1, ETagOrBlockId = "local" }]
            };
            await multipart.CompleteAsync(completed, ct)
                .ConfigureAwait(false);
        }
        catch {
            await multipart.AbortAsync(begin.SessionId, CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private static FileManagerEntry ToEntry(FileStorageFolderEntry entry)
        => new(
            entry.Name, entry.IsDirectory, entry.PathPrefix, entry.FileId, (ApiPresence)(int)entry.Presence, entry.OriginalFileSize, entry.PhysicalKey, false,
            entry.IsCompressed, entry.Compression, entry.IsEncrypted, entry.Encryption);

    private static IKeyStore? CreateKeyStore(FileManagerServer server)
        => BuildKeyStore(server.KeyStoreKind, server.Passphrase, server.Keys, server.AwsRegion, server.AwsSecretPrefix, server.AwsAccessKeyId, server.AwsSecretAccessKey);

    internal static IKeyStore? CreateKeyStore(FileManagerKeyStore store)
        => BuildKeyStore(store.Kind, store.Passphrase, store.Keys, store.AwsRegion, store.AwsSecretPrefix, store.AwsAccessKeyId, store.AwsSecretAccessKey);

    private static IKeyStore? BuildKeyStore(
        FileManagerKeyStoreKind kind,
        string? passphrase,
        List<FileManagerStoredKey>? keys,
        string? awsRegion,
        string? awsSecretPrefix,
        string? awsAccessKeyId,
        string? awsSecretAccessKey)
    {
        if (kind == FileManagerKeyStoreKind.Aws) {
            var region = string.IsNullOrWhiteSpace(awsRegion) ? RegionEndpoint.USEast1 : RegionEndpoint.GetBySystemName(awsRegion);
            var config = new AmazonSecretsManagerConfig { RegionEndpoint = region };
            IAmazonSecretsManager client = string.IsNullOrWhiteSpace(awsAccessKeyId)
                ? new AmazonSecretsManagerClient(config)
                : new AmazonSecretsManagerClient(awsAccessKeyId, awsSecretAccessKey, config);
            var prefix = string.IsNullOrWhiteSpace(awsSecretPrefix) ? "lyo/kek" : awsSecretPrefix.Trim();
            return new AwsKeyStore(client, prefix);
        }

        var store = new LocalKeyStore();
        var any = false;
        if (!string.IsNullOrWhiteSpace(passphrase)) {
            store.AddKeyFromString(FileManagerLimits.LocalKeyId, FileManagerLimits.LocalKeyVersion, passphrase);
            any = true;
        }

        foreach (var key in keys ?? []) {
            if (string.IsNullOrWhiteSpace(key.Id))
                continue;
            var id = key.Id.Trim();
            var version = string.IsNullOrWhiteSpace(key.Version) ? "1" : key.Version.Trim();
            if (!string.IsNullOrWhiteSpace(key.RawKey)) {
                store.AddKey(id, version, Convert.FromBase64String(key.RawKey));
                if (key.Metadata != null)
                    store.SetKeyMetadata(id, version, key.Metadata);
                if (key.Current)
                    store.SetCurrentVersion(id, version);
                any = true;
                continue;
            }

            if (string.IsNullOrWhiteSpace(key.Secret))
                continue;
            store.AddKeyFromString(id, version, key.Secret);
            any = true;
        }

        return any ? store : null;
    }

    internal static void Migrate(IServiceProvider services, FileManagerMetadataKind kind)
    {
        if (kind == FileManagerMetadataKind.Sqlite) {
            using var db = services.GetRequiredService<IDbContextFactory<SqliteFileMetadataStoreDbContext>>().CreateDbContext();
            db.Database.Migrate();
            return;
        }

        using var postgres = services.GetRequiredService<IDbContextFactory<FileMetadataStoreDbContext>>().CreateDbContext();
        postgres.Database.Migrate();
    }

    private static async Task CopyAsync(Stream source, string destinationPath, CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        await using var dest = File.Create(destinationPath);
        await source.CopyToAsync(dest, ct).ConfigureAwait(false);
    }
}
