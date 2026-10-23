using Lyo.Compression;
using Lyo.Exceptions;
using Lyo.FileMetadataStore;
using Lyo.FileMetadataStore.DownloadAccess;
using Lyo.FileStorage;
using Lyo.FileStorage.AzureBlob;
using Lyo.FileStorage.Ftp;
using Lyo.FileStorage.Models;
using Lyo.FileStorage.Multipart;
using Lyo.FileStorage.S3;
using Lyo.FileStorage.Sftp;
using Lyo.IO.Temp;
using Lyo.KeyStore;
using Lyo.Lock;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.FileManager;

/// <summary>
/// Appsettings stores for the desktop. Each open creates a scope and calls <see cref="IFileStorageFactory.Get" />. Disposing that connection disposes the scope. A second
/// open of the same name gets its own scope.
/// </summary>
public sealed class FileManagerConfiguredHost : IDisposable
{
    private readonly Dictionary<string, FileManagerCatalogPlan> _catalogs;
    private readonly ServiceProvider _provider;

    /// <summary>Stores that can be opened. Skipped sections are absent.</summary>
    public IReadOnlyList<FileManagerServer> Servers { get; }

    /// <summary>Sections that were not registered, each message naming the store and the reason.</summary>
    public IReadOnlyList<string> Skipped { get; }

    private FileManagerConfiguredHost(
        ServiceProvider provider,
        IReadOnlyList<FileManagerServer> servers,
        IReadOnlyList<string> skipped,
        Dictionary<string, FileManagerCatalogPlan> catalogs)
    {
        _provider = provider;
        Servers = servers;
        Skipped = skipped;
        _catalogs = catalogs;
    }

    /// <inheritdoc />
    public void Dispose() => _provider.Dispose();

    /// <summary>Reads <c>FileStorage:Stores</c>. One bad section is skipped. The rest are still registered.</summary>
    public static FileManagerConfiguredHost Load(IConfiguration configuration, ILoggerFactory? logs = null)
    {
        ArgumentHelpers.ThrowIfNull(configuration);
        var services = new ServiceCollection();
        if (logs != null)
            services.AddSingleton(logs);

        services.AddSingleton<ICompressionAlgorithmSelector, UploadCompressionSelector>();
        services.AddCompressionService();
        services.AddIOTempService();
        services.AddLocalLock();
        services.AddInMemoryMultipartUploadSessionStore();
        services.AddScoped<FileManagerOpenKeys>();
        services.AddScoped<FileManagerCatalogLease>();
        var compression = new CompressionHolder();
        var servers = new List<FileManagerServer>();
        var skipped = new List<string>();
        var catalogs = new Dictionary<string, FileManagerCatalogPlan>(StringComparer.OrdinalIgnoreCase);
        services.AddFileStorageFactory(factory => AddStores(factory, configuration.GetSection("FileStorage"), compression, servers, skipped, catalogs));
        var provider = services.BuildServiceProvider();
        compression.Service = provider.GetRequiredService<CompressionService>();
        return new(provider, servers, skipped, catalogs);
    }

    /// <summary>Opens <paramref name="server" /> in a new scope. <paramref name="keyStore" /> is the toolbar key store for this connection.</summary>
    public FileStorageLocalHost Open(FileManagerServer server, FileManagerKeyStore? keyStore = null)
    {
        ArgumentHelpers.ThrowIfNull(server);
        var scope = _provider.CreateScope();
        try {
            var services = scope.ServiceProvider;
            var keys = keyStore == null ? null : FileStorageLocalHost.CreateKeyStore(keyStore);
            services.GetRequiredService<FileManagerOpenKeys>().Store = keys;
            IFileDownloadAccessService access = FileManagerUncatalogedAccess.Instance;
            var sessions = services.GetRequiredService<IMultipartUploadSessionStore>();
            if (_catalogs.TryGetValue(server.Name, out var plan)) {
                var lease = services.GetRequiredService<FileManagerCatalogLease>();
                lease.Open(plan);
                access = lease.Access;
                sessions = lease.Sessions;
            }

            var storage = services.GetRequiredService<IFileStorageFactory>().Get(server.Name);
            return FileStorageLocalHost.FromFactory(server, scope, storage, access, sessions, keys);
        }
        catch {
            scope.Dispose();
            throw;
        }
    }

    private static void AddStores(
        FileStorageFactoryBuilder factory,
        IConfiguration section,
        CompressionHolder compression,
        List<FileManagerServer> servers,
        List<string> skipped,
        Dictionary<string, FileManagerCatalogPlan> catalogs)
    {
        foreach (var child in section.GetSection(FileStorageFactoryKeys.Stores).GetChildren()) {
            try {
                if (!TryPrepare(child, skipped, out var plan))
                    continue;

                Register(factory, child, compression, plan, catalogs);
                servers.Add(
                    new() {
                        Name = child.Key.Trim(),
                        Kind = Kind(child["Provider"] ?? ""),
                        Configured = true,
                        RootDirectory = child["RootDirectoryPath"]?.Trim() ?? "",
                        MetadataKind = plan?.Kind ?? FileManagerMetadataKind.Sqlite,
                        ConnectionString = plan?.ConnectionString
                    });
            }
            catch (Exception ex) {
                skipped.Add(ex.Message);
            }
        }
    }

    private static bool TryPrepare(IConfigurationSection section, List<string> skipped, out FileManagerCatalogPlan? plan)
    {
        plan = null;
        var name = section.Key?.Trim() ?? "";
        if (Reserved(section, name, skipped))
            return false;

        var provider = section["Provider"]?.Trim() ?? "";
        if (provider.Length == 0)
            throw new InvalidOperationException($"File storage '{name}' is missing Provider.");

        if (!Known(provider))
            throw new InvalidOperationException($"File storage '{name}' provider '{provider}' is not registered.");

        var connection = section[FileStorageFactoryKeys.CatalogConnectionString];
        var disk = provider.Equals("disk", StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(connection)) {
            if (disk)
                return true;

            skipped.Add($"File storage '{name}' has no {FileStorageFactoryKeys.CatalogConnectionString}.");
            return false;
        }

        var kindText = section[FileStorageFactoryKeys.MetadataKind];
        if (string.IsNullOrWhiteSpace(kindText))
            throw new InvalidOperationException($"File storage '{name}' MetadataKind is required with {FileStorageFactoryKeys.CatalogConnectionString}.");

        if (!Enum.TryParse(kindText.Trim(), true, out FileManagerMetadataKind kind))
            throw new InvalidOperationException($"File storage '{name}' MetadataKind '{kindText.Trim()}' is not Sqlite or Postgres.");

        var text = connection.Trim();
        if (kind == FileManagerMetadataKind.Sqlite && !text.Contains('='))
            text = "Data Source=" + text;

        plan = new(kind, text);
        return true;
    }

    private static bool Reserved(IConfigurationSection section, string name, List<string> skipped)
    {
        foreach (var key in new[] { FileStorageFactoryKeys.MetadataStoreKey, FileStorageFactoryKeys.EncryptionServiceKey, FileStorageFactoryKeys.CompressionServiceKey }) {
            var value = section[key];
            if (string.IsNullOrWhiteSpace(value))
                continue;

            skipped.Add($"File storage '{name}' skips {key} '{value.Trim()}'.");
            return true;
        }

        return false;
    }

    private static void Register(
        FileStorageFactoryBuilder factory,
        IConfigurationSection section,
        CompressionHolder compression,
        FileManagerCatalogPlan? plan,
        Dictionary<string, FileManagerCatalogPlan> catalogs)
    {
        var name = section.Key.Trim();
        if (plan != null)
            catalogs[name] = plan;

        switch (section["Provider"]!.Trim().ToLowerInvariant()) {
            case "disk":
                var disk = new DiskFileStorageBuilder();
                Prepare(disk, section, name, plan != null, compression);
                factory.Add(name, disk);
                break;
            case "s3":
                var s3 = new S3FileStorageBuilder();
                Prepare(s3, section, name, plan != null, compression);
                factory.Add(name, s3);
                break;
            case "azureblob":
                var azure = new AzureBlobFileStorageBuilder();
                Prepare(azure, section, name, plan != null, compression);
                factory.Add(name, azure);
                break;
            case "ftp":
                var ftp = new FtpFileStorageBuilder();
                Prepare(ftp, section, name, plan != null, compression);
                factory.Add(name, ftp);
                break;
            default:
                var sftp = new SftpFileStorageBuilder();
                Prepare(sftp, section, name, plan != null, compression);
                factory.Add(name, sftp);
                break;
        }
    }

    private static void Prepare<TOptions, TBuilder>(
        FileStorageServiceBuilder<TOptions, TBuilder> builder,
        IConfigurationSection section,
        string name,
        bool catalog,
        CompressionHolder compression)
        where TOptions : FileStorageServiceBaseOptions, new() where TBuilder : FileStorageServiceBuilder<TOptions, TBuilder>
    {
        builder.ApplySection(section);
        builder.WithCompression(_ => compression.Service);
        builder.WithEncryption(static services => {
            var keys = services.GetRequiredService<FileManagerOpenKeys>().Store;
            return keys == null ? null! : FileManagerEncryption.Create(keys);
        });

        if (!catalog)
            return;

        builder.WithMetadata(services
            => services.GetRequiredService<FileManagerCatalogLease>().Store ?? throw new InvalidOperationException($"File storage '{name}' catalog is not open."));
    }

    private static bool Known(string provider)
        => provider.Equals("disk", StringComparison.OrdinalIgnoreCase) || provider.Equals("s3", StringComparison.OrdinalIgnoreCase) ||
            provider.Equals("azureblob", StringComparison.OrdinalIgnoreCase) || provider.Equals("ftp", StringComparison.OrdinalIgnoreCase) ||
            provider.Equals("sftp", StringComparison.OrdinalIgnoreCase);

    private static FileManagerServerKind Kind(string provider)
        => provider.Trim().ToLowerInvariant() switch {
            "s3" => FileManagerServerKind.S3,
            "azureblob" => FileManagerServerKind.AzureBlob,
            "ftp" => FileManagerServerKind.Ftp,
            "sftp" => FileManagerServerKind.Sftp,
            var _ => FileManagerServerKind.Local
        };

    private sealed class CompressionHolder
    {
        public CompressionService Service { get; set; } = null!;
    }
}

internal sealed record FileManagerCatalogPlan(FileManagerMetadataKind Kind, string ConnectionString);

internal sealed class FileManagerOpenKeys
{
    public IKeyStore? Store { get; set; }
}

internal sealed class FileManagerCatalogLease : IDisposable
{
    private ServiceProvider? _provider;
    private IServiceScope? _scope;

    public IFileMetadataStore Store { get; private set; } = null!;

    public IFileDownloadAccessService Access { get; private set; } = null!;

    public IMultipartUploadSessionStore Sessions { get; private set; } = null!;

    public void Dispose()
    {
        _scope?.Dispose();
        _scope = null;
        _provider?.Dispose();
        _provider = null;
    }

    public void Open(FileManagerCatalogPlan plan)
    {
        var services = new ServiceCollection();
        FileStorageLocalHost.AddCatalog(services, plan.Kind, plan.ConnectionString);
        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        FileStorageLocalHost.Migrate(_scope.ServiceProvider, plan.Kind);
        Store = FileStorageLocalHost.CatalogMetadata(_scope.ServiceProvider, plan.Kind);
        Access = _scope.ServiceProvider.GetRequiredService<IFileDownloadAccessService>();
        Sessions = _scope.ServiceProvider.GetRequiredService<IMultipartUploadSessionStore>();
    }
}

internal sealed class FileManagerUncatalogedAccess : IFileDownloadAccessService
{
    public static FileManagerUncatalogedAccess Instance { get; } = new();

    public Task<CreateFileDownloadAccessLinkResult> CreateLinkAsync(CreateFileDownloadAccessLinkRequest request, CancellationToken ct = default)
        => throw new InvalidOperationException("This store has no catalog database.");

    public Task<ConsumeFileDownloadAccessLinkResult> ValidateAndConsumeDownloadAsync(
        string token,
        string? actorId = null,
        string? ipAddress = null,
        DateTime? nowUtc = null,
        CancellationToken ct = default)
        => throw new InvalidOperationException("This store has no catalog database.");
}