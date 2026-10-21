using Lyo.Api.FileStorage.Models;
using Lyo.Exceptions;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage;
using Microsoft.Extensions.Logging;
using FileStoragePresence = Lyo.Api.FileStorage.Models.FileStoragePresence;

namespace Lyo.FileManager;

/// <summary>Server list, the open folder, and file, folder, transfer, and key actions.</summary>
public sealed class FileManagerSession : IDisposable
{
    private readonly List<FileManagerConnection> _connections = [];
    private readonly List<FileManagerKeyStore> _keyStores = [];
    private readonly Func<FileManagerServer, IFileManagerApi> _open;
    private readonly List<FileManagerServer> _servers = [];
    private FileManagerConfiguredHost? _configured;
    private bool _keyStoresLoaded;
    private string? _looseError;

    /// <summary>Logger factory for a local host. Null keeps library logs off the screen.</summary>
    public ILoggerFactory? Logs { get; set; }

    /// <summary>Saved servers.</summary>
    public IReadOnlyList<FileManagerServer> Servers => _servers;

    /// <summary>Saved key stores. Connect uses the one named by <see cref="FileManagerServer.KeyStoreId" />.</summary>
    public IReadOnlyList<FileManagerKeyStore> KeyStores => _keyStores;

    /// <summary>Server the tree and actions use.</summary>
    public FileManagerServer? Active { get; private set; }

    /// <summary>Open server and key store pairs. Each one keeps its own folder.</summary>
    public IReadOnlyList<FileManagerConnection> Connections => _connections;

    /// <summary>Open folder on the active connection. Null is the root.</summary>
    public string? Prefix => Current?.Prefix;

    /// <summary>Children of <see cref="Prefix" /> on the active connection.</summary>
    public IReadOnlyList<FileManagerEntry> Entries => Current?.Entries ?? [];

    /// <summary>True when the last list on the active connection was capped.</summary>
    public bool Truncated => Current?.Truncated ?? false;

    /// <summary>Last connect or action error on the active connection. Null when the last call succeeded.</summary>
    public string? Error {
        get => Current?.Error ?? _looseError;
        private set {
            if (Current != null)
                Current.Error = value;
            else
                _looseError = value;
        }
    }

    /// <summary>Feature flags from the last successful connect. Every flag is false until then.</summary>
    public FileStorageCapabilitiesResponse Capabilities {
        get => Current?.Capabilities ?? FileStorageCapabilitiesResponse.None;
        private set {
            if (Current != null)
                Current.Capabilities = value;
        }
    }

    /// <summary>Rows from the last query on the active connection.</summary>
    public IReadOnlyList<FileManagerQueryRow> QueryRows {
        get => Current?.QueryRows ?? [];
        private set {
            if (Current != null)
                Current.QueryRows = value;
        }
    }

    /// <summary>Counts from the last bulk action.</summary>
    public FileManagerActionResult? LastAction { get; private set; }

    private FileManagerConnection? Current
        => Active == null ? null : _connections.FirstOrDefault(connection => connection.ServerId == Active.Id && connection.KeyStoreId == OpenKey(Active));

    /// <summary>Opens each server through <c>FileManagerApis.Open</c>. <see cref="Logs" /> is passed into a local host.</summary>
    public FileManagerSession() => _open = OpenSaved;

    /// <summary>Opens each server with <paramref name="open" />. Tests pass a fake.</summary>
    public FileManagerSession(Func<FileManagerServer, IFileManagerApi> open)
    {
        ArgumentHelpers.ThrowIfNull(open);
        _open = open;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var connection in _connections)
            connection.Api.Dispose();

        _connections.Clear();
        _configured?.Dispose();
        _configured = null;
    }

    /// <summary>Adds a remote server and selects it.</summary>
    public FileManagerServer AddRemote(string name, string baseUrl, string? route = null, string? metadataRoute = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(baseUrl);
        EnsureUnique(name);
        var server = new FileManagerServer {
            Name = name.Trim(),
            Kind = FileManagerServerKind.Remote,
            BaseUrl = baseUrl.Trim(),
            Route = string.IsNullOrWhiteSpace(route) ? "FileStorage" : route.Trim().Trim('/'),
            FileMetadataRoute = metadataRoute?.Trim().Trim('/') ?? ""
        };

        _servers.Add(server);
        Active = server;
        return server;
    }

    /// <summary>Adds a local server and selects it.</summary>
    public FileManagerServer AddLocal(string name, string rootDirectory, FileManagerMetadataKind metadata, string? connectionString = null, string? passphrase = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(rootDirectory);
        EnsureUnique(name);
        var server = new FileManagerServer {
            Name = name.Trim(),
            Kind = FileManagerServerKind.Local,
            RootDirectory = rootDirectory.Trim(),
            MetadataKind = metadata,
            ConnectionString = string.IsNullOrWhiteSpace(connectionString) ? null : connectionString.Trim(),
            Passphrase = string.IsNullOrWhiteSpace(passphrase) ? null : passphrase
        };

        _servers.Add(server);
        Active = server;
        return server;
    }

    /// <summary>Adds a saved server and selects it. Provider fields on <paramref name="server" /> are kept.</summary>
    public FileManagerServer Add(FileManagerServer server)
    {
        ArgumentHelpers.ThrowIfNull(server);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(server.Name);
        EnsureUnique(server.Name);
        _servers.Add(server);
        Active = server;
        return server;
    }

    /// <summary>Replaces the key store catalog. Used when settings load. After this, connect uses <see cref="FileManagerServer.KeyStoreId" /> and ignores a passphrase left on the server.</summary>
    public void ReplaceKeyStores(IReadOnlyList<FileManagerKeyStore> stores)
    {
        ArgumentHelpers.ThrowIfNull(stores);
        _keyStores.Clear();
        _keyStores.AddRange(stores);
        _keyStoresLoaded = true;
    }

    /// <summary>The catalog entry selected for <paramref name="server" />. Null when none is selected.</summary>
    public FileManagerKeyStore? KeyStoreFor(FileManagerServer? server)
    {
        if (server?.KeyStoreId is not Guid id)
            return null;

        return _keyStores.FirstOrDefault(store => store.Id == id);
    }

    /// <summary>Adds appsettings stores. A name that matches a saved server throws. The active server stays when one is already selected.</summary>
    public void UseConfiguredStores(FileManagerConfiguredHost host)
    {
        ArgumentHelpers.ThrowIfNull(host);
        if (_configured != null)
            throw new InvalidOperationException("Configured stores are already loaded.");

        _configured = host;
        foreach (var server in host.Servers)
            Include(server);
    }

    /// <summary>Replaces the list. Used when settings load.</summary>
    public void ReplaceServers(IReadOnlyList<FileManagerServer> servers)
    {
        ArgumentHelpers.ThrowIfNull(servers);
        _servers.Clear();
        _servers.AddRange(servers);
        Active = _servers.FirstOrDefault();
    }

    /// <summary>Selects a saved server. Does not connect.</summary>
    public void Select(Guid id) => Active = Find(id);

    /// <summary>Removes a server and closes every connection that opened it. The active server becomes the first one left.</summary>
    public void Remove(Guid id)
    {
        CloseServer(id);
        _servers.RemoveAll(server => server.Id == id);
        if (Active?.Id == id)
            Active = _connections.FirstOrDefault()?.Server ?? _servers.FirstOrDefault();
    }

    /// <summary>Disposes the open pair of <paramref name="serverId" /> and <paramref name="keyStoreId" />. The saved server stays.</summary>
    public void CloseConnection(Guid serverId, Guid? keyStoreId)
    {
        var index = _connections.FindIndex(connection => connection.ServerId == serverId && connection.KeyStoreId == keyStoreId);
        if (index < 0)
            return;

        var connection = _connections[index];
        _connections.RemoveAt(index);
        connection.Api.Dispose();
        if (Active?.Id != serverId || OpenKey(Active) != keyStoreId)
            return;

        var next = _connections.FirstOrDefault(item => item.ServerId == serverId) ?? _connections.FirstOrDefault();
        if (next == null) {
            Active = _servers.FirstOrDefault(server => server.Id != serverId);
            return;
        }

        RememberKey(next);
        Active = next.Server;
    }

    /// <summary>Renames a server.</summary>
    public void Rename(Guid id, string name)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        EnsureUnique(name, id);
        Find(id).Name = name.Trim();
    }

    /// <summary>Stores a bearer token or API key on the active remote server.</summary>
    public void SetAuth(FileManagerAuthKind kind, string? secret)
    {
        var server = RequireActive();
        server.AuthKind = kind;
        server.Secret = string.IsNullOrWhiteSpace(secret) ? null : secret.Trim();
    }

    /// <summary>Opens the active server. A failed probe sets <see cref="Error" /> and leaves the tree.</summary>
    public async Task ConnectAsync(CancellationToken ct)
    {
        var server = RequireActive();
        var api = _open(server);
        var keyId = OpenKey(server);
        var existing = _connections.FirstOrDefault(connection => connection.ServerId == server.Id && connection.KeyStoreId == keyId);
        try {
            var health = await api.CheckHealthAsync(ct).ConfigureAwait(false);
            if (!health.IsHealthy) {
                if (existing == null || !ReferenceEquals(api, existing.Api))
                    api.Dispose();

                Error = string.IsNullOrWhiteSpace(health.Message) ? "Unhealthy." : health.Message;
                return;
            }

            if (existing == null) {
                var keyName = keyId is Guid id ? _keyStores.FirstOrDefault(item => item.Id == id)?.Name : null;
                existing = new(server, api, keyId, keyName);
                _connections.Add(existing);
            }
            else if (!ReferenceEquals(api, existing.Api)) {
                existing.Api.Dispose();
                existing.Api = api;
            }

            _looseError = null;
            existing.Error = null;
            existing.Capabilities = await api.GetCapabilitiesAsync(ct).ConfigureAwait(false);
            Active = server;
            await ReloadAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            if (existing == null || !ReferenceEquals(api, existing.Api))
                api.Dispose();

            Error = ex.Message;
        }
    }

    /// <summary>Reloads <see cref="Prefix" /> and drops client-only folders.</summary>
    public Task RefreshAsync(CancellationToken ct)
    {
        RequireConnection().Pending.Clear();
        return ReloadAsync(ct);
    }

    /// <summary>Directory prefixes on the open connection for <paramref name="serverId" />. Does not change the visible folder.</summary>
    public async Task<IReadOnlyList<string>> FolderPrefixesAsync(Guid serverId, CancellationToken ct)
    {
        var connection = _connections.FirstOrDefault(item => item.ServerId == serverId) ?? throw new InvalidOperationException("Connect that server before listing folders.");
        var found = new List<string>();
        var pending = new Queue<string?>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        pending.Enqueue(null);
        while (pending.Count > 0 && found.Count < 200) {
            ct.ThrowIfCancellationRequested();
            var prefix = pending.Dequeue();
            var listed = await connection.Api.ListAsync(prefix, ct).ConfigureAwait(false);
            foreach (var entry in listed.Entries) {
                if (!entry.IsDirectory)
                    continue;

                var path = string.IsNullOrWhiteSpace(entry.PathPrefix) ? entry.Name : entry.PathPrefix.Trim().Trim('/');
                if (string.IsNullOrWhiteSpace(path) || !seen.Add(path))
                    continue;

                found.Add(path);
                pending.Enqueue(path);
            }
        }

        return found;
    }

    /// <summary>Lists one prefix without changing <see cref="Prefix" />. Pending folders whose parent is that prefix are included.</summary>
    public async Task<FileManagerListing> ListAsync(string? pathPrefix, CancellationToken ct)
    {
        var connection = RequireConnection();
        var listed = await connection.Api.ListAsync(pathPrefix, ct).ConfigureAwait(false);
        var entries = MergePending(connection, listed.Entries, pathPrefix);
        return entries.Count == listed.Entries.Count ? listed : new(entries, listed.Truncated);
    }

    /// <summary>Opens <paramref name="prefix" />. Pending folders stay until <see cref="RefreshAsync" />.</summary>
    public async Task OpenAsync(string? prefix, CancellationToken ct)
    {
        var connection = RequireConnection();
        connection.Prefix = string.IsNullOrWhiteSpace(prefix) ? null : prefix.Trim().Trim('/');
        await ReloadAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Opens a directory.</summary>
    public async Task EnterAsync(FileManagerEntry entry, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(entry);
        if (!entry.IsDirectory)
            return;

        RequireConnection().Prefix = entry.PathPrefix;
        await ReloadAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Opens the parent prefix.</summary>
    public async Task UpAsync(CancellationToken ct)
    {
        var connection = RequireConnection();
        connection.Prefix = PrefixRewrite.Parent(connection.Prefix);
        await ReloadAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Shows an empty folder until the next refresh. Entering it and coming back up still shows it.</summary>
    public void NewFolder(string name)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        var connection = RequireConnection();
        var segment = name.Trim().Trim('/');
        var prefix = string.IsNullOrEmpty(connection.Prefix) ? segment : connection.Prefix + "/" + segment;
        var entry = new FileManagerEntry(segment, true, prefix, null, FileStoragePresence.None, 0, null, true);
        connection.Pending.Add(entry);
        connection.Entries = [.. connection.Entries, entry];
    }

    /// <summary>Runs a name and prefix query.</summary>
    public async Task QueryAsync(string? nameContains, string? pathPrefix, CancellationToken ct)
    {
        QueryRows = await RequireApi().QueryAsync(nameContains, pathPrefix, ct).ConfigureAwait(false);
        Error = null;
    }

    /// <summary>Uploads a file into <see cref="Prefix" />.</summary>
    public Task UploadAsync(string filePath, FileManagerUploadOptions? options, CancellationToken ct) => UploadAsync(filePath, Current?.Prefix, options, ct);

    /// <summary>Uploads a file onto <paramref name="pathPrefix" /> and leaves <see cref="Prefix" /> unchanged.</summary>
    public async Task UploadAsync(string filePath, string? pathPrefix, FileManagerUploadOptions? options, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(filePath);
        await RequireApi().UploadAsync(filePath, EmptyToNull(pathPrefix), options ?? new(), ct).ConfigureAwait(false);
        await ReloadAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Downloads a catalog file or a physical object.</summary>
    public Task DownloadAsync(FileManagerEntry entry, string destinationPath, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(entry);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(destinationPath);
        var api = RequireApi();
        return entry.FileId is Guid id ? api.DownloadAsync(id, destinationPath, ct) : api.DownloadPhysicalAsync(entry.PhysicalKey ?? entry.Name, destinationPath, ct);
    }

    /// <summary>Zips catalog files, or every file under a folder.</summary>
    public async Task DownloadArchiveAsync(IReadOnlyList<FileManagerEntry> entries, string destinationPath, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(entries);
        var ids = await CatalogIdsAsync(entries, ct).ConfigureAwait(false);
        await RequireApi().DownloadArchiveAsync(ids, destinationPath, ct).ConfigureAwait(false);
    }

    /// <summary>Renames a catalog file or a physical object, then reloads.</summary>
    public Task<FileManagerActionResult> RenameAsync(FileManagerEntry entry, string name, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(entry);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        if (entry.FileId is Guid id)
            return RunAsync([token => RequireApi().RenameAsync(id, name.Trim(), token)], ct);

        var key = entry.PhysicalKey ?? entry.Name;
        return RunAsync([token => RequireApi().RenamePhysicalAsync(key, name.Trim(), token)], ct);
    }

    /// <summary>Moves catalog files onto one prefix, then reloads.</summary>
    public Task<FileManagerActionResult> MoveAsync(IReadOnlyList<FileManagerEntry> files, string? destination, CancellationToken ct)
        => RunFilesAsync(files, (api, id, token) => api.MoveAsync(id, EmptyToNull(destination), token), ct);

    /// <summary>Copies catalog files onto one prefix, then reloads.</summary>
    public Task<FileManagerActionResult> CopyAsync(IReadOnlyList<FileManagerEntry> files, string? destination, CancellationToken ct)
        => RunFilesAsync(files, (api, id, token) => api.CopyAsync(id, EmptyToNull(destination), token), ct);

    /// <summary>Deletes catalog files, then reloads.</summary>
    public Task<FileManagerActionResult> DeleteAsync(IReadOnlyList<FileManagerEntry> files, CancellationToken ct)
        => RunFilesAsync(files, (api, id, token) => api.DeleteAsync(id, token), ct);

    /// <summary>Moves a folder. Nested prefixes stay nested under the destination.</summary>
    public async Task<FileManagerActionResult> MoveFolderAsync(FileManagerEntry folder, string destination, CancellationToken ct)
    {
        var files = await FilesUnderAsync(folder, ct).ConfigureAwait(false);
        var source = folder.PathPrefix ?? "";
        var result = await RunAsync(
                files.Select(file => (Func<CancellationToken, Task>)(token
                        => RequireApi().MoveAsync(file.FileId!.Value, PrefixRewrite.Rewrite(source, destination, file.PathPrefix), token)))
                    .ToList(), ct)
            .ConfigureAwait(false);

        if (result.Failed == 0)
            RelocatePending(RequireConnection(), source, destination, false);

        return result;
    }

    /// <summary>Copies a folder. Nested prefixes stay nested under the destination.</summary>
    public async Task<FileManagerActionResult> CopyFolderAsync(FileManagerEntry folder, string destination, CancellationToken ct)
    {
        var files = await FilesUnderAsync(folder, ct).ConfigureAwait(false);
        var source = folder.PathPrefix ?? "";
        var result = await RunAsync(
                files.Select(file => (Func<CancellationToken, Task>)(token
                        => RequireApi().CopyAsync(file.FileId!.Value, PrefixRewrite.Rewrite(source, destination, file.PathPrefix), token)))
                    .ToList(), ct)
            .ConfigureAwait(false);

        if (result.Failed == 0)
            RelocatePending(RequireConnection(), source, destination, true);

        return result;
    }

    /// <summary>Deletes every catalog file under a folder.</summary>
    public async Task<FileManagerActionResult> DeleteFolderAsync(FileManagerEntry folder, CancellationToken ct)
    {
        var files = await FilesUnderAsync(folder, ct).ConfigureAwait(false);
        return await RunAsync(files.Select(file => (Func<CancellationToken, Task>)(token => RequireApi().DeleteAsync(file.FileId!.Value, token))).ToList(), ct)
            .ConfigureAwait(false);
    }

    /// <summary>Moves, copies, renames, or deletes physical objects, then reloads.</summary>
    public Task<FileManagerActionResult> MovePhysicalAsync(IReadOnlyList<FileManagerEntry> files, string? destination, CancellationToken ct)
        => RunPhysicalAsync(files, (api, key, token) => api.MovePhysicalAsync(key, EmptyToNull(destination), token), ct);

    /// <summary>Copies physical objects, then reloads.</summary>
    public Task<FileManagerActionResult> CopyPhysicalAsync(IReadOnlyList<FileManagerEntry> files, string? destination, CancellationToken ct)
        => RunPhysicalAsync(files, (api, key, token) => api.CopyPhysicalAsync(key, EmptyToNull(destination), token), ct);

    /// <summary>Deletes physical objects, then reloads.</summary>
    public Task<FileManagerActionResult> DeletePhysicalAsync(IReadOnlyList<FileManagerEntry> files, CancellationToken ct)
        => RunPhysicalAsync(files, (api, key, token) => api.DeletePhysicalAsync(key, token), ct);

    /// <summary>Creates an access link for a catalog file.</summary>
    public Task<string> CreateAccessLinkAsync(Guid fileId, DateTime? expiresAtUtc, int? maxDownloads, CancellationToken ct)
        => RequireApi().CreateAccessLinkAsync(fileId, expiresAtUtc, maxDownloads, ct);

    /// <summary>Returns a presigned read URL.</summary>
    public Task<string> PresignedReadAsync(Guid fileId, CancellationToken ct) => RequireApi().PresignedReadAsync(fileId, ct);

    /// <summary>Loads metadata.</summary>
    public Task<FileStoreResult> GetMetadataAsync(Guid fileId, bool includeDeleted, CancellationToken ct) => RequireApi().GetMetadataAsync(fileId, includeDeleted, ct);

    /// <summary>Lists key ids.</summary>
    public Task<IReadOnlyList<string>> ListKeyIdsAsync(CancellationToken ct) => RequireApi().ListKeyIdsAsync(ct);

    /// <summary>Rotates the selection, or every file in a folder.</summary>
    public async Task<DekMigrationResult> RotateAsync(IReadOnlyList<FileManagerEntry> entries, string? targetKeyId, string? targetKeyVersion, CancellationToken ct)
    {
        var ids = await CatalogIdsAsync(entries, ct).ConfigureAwait(false);
        var result = await RequireApi().RotateAsync(ids, EmptyToNull(targetKeyId), EmptyToNull(targetKeyVersion), ct).ConfigureAwait(false);
        await ReloadAsync(ct).ConfigureAwait(false);
        return result;
    }

    /// <summary>Migrates files off a source key.</summary>
    public async Task<DekMigrationResult> MigrateAsync(string sourceKeyId, string? sourceKeyVersion, string? targetKeyId, string? targetKeyVersion, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sourceKeyId);
        var result = await RequireApi()
            .MigrateAsync(sourceKeyId.Trim(), EmptyToNull(sourceKeyVersion), EmptyToNull(targetKeyId), EmptyToNull(targetKeyVersion), ct)
            .ConfigureAwait(false);

        await ReloadAsync(ct).ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Copies or moves catalog files onto another open connection. Move tombstones the source only after the destination hashes match. A remote connection throws before any
    /// write. <paramref name="destinationPrefix" /> places the copies in that folder. Blank keeps each file's current prefix.
    /// </summary>
    public async Task<FileStorageTransferResult> TransferAsync(
        Guid destinationServerId,
        IReadOnlyList<FileManagerEntry> entries,
        FileStorageTransferDisposition disposition,
        FileStorageKeyMode keyMode = FileStorageKeyMode.PreserveKeys,
        string? targetKeyId = null,
        string? targetKeyVersion = null,
        IProgress<FileStorageTransferProgress>? progress = null,
        CancellationToken ct = default,
        string? destinationPrefix = null)
    {
        ArgumentHelpers.ThrowIfNull(entries);
        var source = RequireConnection();
        if (destinationServerId == source.ServerId)
            throw new InvalidOperationException("Transfer needs a different connection.");

        var destination = _connections.FirstOrDefault(connection => connection.ServerId == destinationServerId) ??
            throw new InvalidOperationException("Connect the destination before transferring.");

        if (source.Api is not IFileManagerTransfer sourceTransfer || destination.Api is not IFileManagerTransfer destinationTransfer)
            throw new InvalidOperationException("Transfer needs two in-process file stores.");

        var ids = await CatalogIdsAsync(entries, ct).ConfigureAwait(false);
        if (ids.Count == 0)
            throw new InvalidOperationException("Select a catalog file.");

        var request = new FileStorageTransferRequest(FileStorageTransferScope.Everything, keyMode, targetKeyId, targetKeyVersion, Disposition: disposition, FileIds: ids.ToArray());
        var result = await FileStorageTransfer.TransferAsync(sourceTransfer.Endpoint, destinationTransfer.Endpoint, request, progress, ct).ConfigureAwait(false);
        var prefix = EmptyToNull(destinationPrefix);
        if (prefix != null) {
            foreach (var id in ids)
                await destination.Api.MoveAsync(id, prefix, ct).ConfigureAwait(false);
        }

        await ReloadConnectionAsync(source, ct).ConfigureAwait(false);
        await ReloadConnectionAsync(destination, ct).ConfigureAwait(false);
        return result;
    }

    /// <summary>Other open in-process stores the active connection can transfer to.</summary>
    public IReadOnlyList<FileManagerServer> TransferDestinations()
    {
        if (Current?.Api is not IFileManagerTransfer)
            return [];

        return _connections.Where(connection => connection.ServerId != Active!.Id && connection.Api is IFileManagerTransfer).Select(connection => connection.Server).ToArray();
    }

    private async Task<FileManagerActionResult> RunFilesAsync(
        IReadOnlyList<FileManagerEntry> files,
        Func<IFileManagerApi, Guid, CancellationToken, Task> call,
        CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(files);
        var api = RequireApi();
        List<Func<CancellationToken, Task>> steps = [];
        foreach (var file in files) {
            if (file.FileId is not Guid id)
                continue;

            steps.Add(token => call(api, id, token));
        }

        return await RunAsync(steps, ct).ConfigureAwait(false);
    }

    private async Task<FileManagerActionResult> RunPhysicalAsync(
        IReadOnlyList<FileManagerEntry> files,
        Func<IFileManagerApi, string, CancellationToken, Task> call,
        CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(files);
        var api = RequireApi();
        List<Func<CancellationToken, Task>> steps = [];
        foreach (var file in files) {
            var key = file.PhysicalKey;
            if (string.IsNullOrWhiteSpace(key))
                continue;

            steps.Add(token => call(api, key, token));
        }

        return await RunAsync(steps, ct).ConfigureAwait(false);
    }

    private async Task<FileManagerActionResult> RunAsync(IReadOnlyList<Func<CancellationToken, Task>> steps, CancellationToken ct)
    {
        var errors = new List<string>();
        var succeeded = 0;
        foreach (var step in steps) {
            try {
                await step(ct).ConfigureAwait(false);
                succeeded++;
            }
            catch (Exception ex) {
                errors.Add(ex.Message);
            }
        }

        var result = new FileManagerActionResult(succeeded, errors.Count, errors);
        LastAction = result;
        Error = errors.Count == 0 ? null : $"{succeeded} succeeded, {errors.Count} failed.";
        if (succeeded > 0)
            await ReloadAsync(ct).ConfigureAwait(false);

        return result;
    }

    private async Task<List<FileManagerEntry>> FilesUnderAsync(FileManagerEntry folder, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(folder);
        if (!folder.IsDirectory || string.IsNullOrWhiteSpace(folder.PathPrefix))
            return [];

        var found = new List<FileManagerEntry>();
        var pending = new Stack<string>();
        pending.Push(folder.PathPrefix);
        while (pending.Count > 0) {
            var prefix = pending.Pop();
            var listed = await RequireApi().ListAsync(prefix, ct).ConfigureAwait(false);
            foreach (var entry in listed.Entries) {
                if (entry.Pending)
                    continue;

                if (entry.IsDirectory && !string.IsNullOrWhiteSpace(entry.PathPrefix))
                    pending.Push(entry.PathPrefix);
                else if (entry.FileId != null)
                    found.Add(entry);
            }
        }

        return found;
    }

    private async Task<IReadOnlyList<Guid>> CatalogIdsAsync(IReadOnlyList<FileManagerEntry> entries, CancellationToken ct)
    {
        List<Guid> ids = [];
        foreach (var entry in entries) {
            if (entry.IsDirectory)
                ids.AddRange((await FilesUnderAsync(entry, ct).ConfigureAwait(false)).Select(file => file.FileId!.Value));
            else if (entry.FileId is Guid id)
                ids.Add(id);
        }

        return ids;
    }

    private Task ReloadAsync(CancellationToken ct) => ReloadConnectionAsync(RequireConnection(), ct);

    private async Task ReloadConnectionAsync(FileManagerConnection connection, CancellationToken ct)
    {
        var listed = await connection.Api.ListAsync(connection.Prefix, ct).ConfigureAwait(false);
        connection.Entries = MergePending(connection, listed.Entries, connection.Prefix);
        connection.Truncated = listed.Truncated;
    }

    private static void RelocatePending(FileManagerConnection connection, string sourceFolder, string destinationFolder, bool copy)
    {
        var pending = connection.Pending;
        List<FileManagerEntry> copies = [];
        for (var i = 0; i < pending.Count; i++) {
            var entry = pending[i];
            var next = PrefixRewrite.Rewrite(sourceFolder, destinationFolder, entry.PathPrefix);
            if (SamePrefix(next, entry.PathPrefix))
                continue;

            var moved = entry with { Name = Segment(next), PathPrefix = next };
            if (copy) {
                if (pending.All(item => !SamePrefix(item.PathPrefix, next)) && copies.All(item => !SamePrefix(item.PathPrefix, next)))
                    copies.Add(moved);
            }
            else
                pending[i] = moved;
        }

        pending.AddRange(copies);
    }

    private static IReadOnlyList<FileManagerEntry> MergePending(FileManagerConnection connection, IReadOnlyList<FileManagerEntry> listed, string? prefix)
    {
        var parent = string.IsNullOrWhiteSpace(prefix) ? null : prefix.Trim().Trim('/');
        var extras = connection.Pending.Where(entry => PrefixRewrite.Parent(entry.PathPrefix) == parent && listed.All(item => item.PathPrefix != entry.PathPrefix)).ToArray();
        return extras.Length == 0 ? listed : [.. listed, .. extras];
    }

    private static bool SamePrefix(string? left, string? right) => string.Equals(left?.Trim().Trim('/') ?? "", right?.Trim().Trim('/') ?? "", StringComparison.Ordinal);

    private static string Segment(string? prefix)
    {
        var trimmed = prefix?.Trim().Trim('/') ?? "";
        var slash = trimmed.LastIndexOf('/');
        return slash < 0 ? trimmed : trimmed[(slash + 1)..];
    }

    private FileManagerServer Find(Guid id) => _servers.FirstOrDefault(s => s.Id == id) ?? throw new InvalidOperationException("Server was not found.");

    private FileManagerServer RequireActive() => Active ?? throw new InvalidOperationException("Select a server.");

    private void CloseServer(Guid id)
    {
        for (var index = _connections.Count - 1; index >= 0; index--) {
            if (_connections[index].ServerId != id)
                continue;

            var connection = _connections[index];
            _connections.RemoveAt(index);
            connection.Api.Dispose();
        }
    }

    private static void RememberKey(FileManagerConnection connection)
    {
        if (connection.Server.Kind != FileManagerServerKind.Remote)
            connection.Server.KeyStoreId = connection.KeyStoreId;
    }

    private static Guid? OpenKey(FileManagerServer server) => server.Kind == FileManagerServerKind.Remote ? null : server.KeyStoreId;

    private FileManagerConnection RequireConnection() => Current ?? throw new InvalidOperationException("Connect before using the server.");

    private IFileManagerApi RequireApi() => RequireConnection().Api;

    private IFileManagerApi OpenSaved(FileManagerServer server)
    {
        if (server.Configured)
            return OpenConfigured(server);

        if (!_keyStoresLoaded)
            return FileManagerApis.Open(server, Logs);

        var store = server.KeyStoreId is Guid id ? _keyStores.FirstOrDefault(item => item.Id == id) : null;
        return FileManagerApis.Open(server, Logs, store);
    }

    private IFileManagerApi OpenConfigured(FileManagerServer server)
    {
        if (_configured == null)
            throw new InvalidOperationException($"File storage '{server.Name}' is not registered.");

        var store = _keyStoresLoaded && server.KeyStoreId is Guid id ? _keyStores.FirstOrDefault(item => item.Id == id) : null;
        return _configured.Open(server, store);
    }

    private void Include(FileManagerServer server)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(server.Name);
        EnsureUnique(server.Name);
        server.Configured = true;
        _servers.Add(server);
        Active ??= server;
    }

    private void EnsureUnique(string name, Guid? except = null)
    {
        var trimmed = name.Trim();
        if (_servers.Any(server => server.Id != except && string.Equals(server.Name, trimmed, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"File storage '{trimmed}' is already registered.");
    }

    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>Picks the HTTP gateway or the local host.</summary>
public static class FileManagerApis
{
    /// <summary>Opens <paramref name="server" />. An in-process server uses the passphrase and keys saved on it.</summary>
    public static IFileManagerApi Open(FileManagerServer server, ILoggerFactory? logs = null)
    {
        ArgumentHelpers.ThrowIfNull(server);
        return server.Kind == FileManagerServerKind.Remote ? new FileStorageHttpApi(server) : FileStorageLocalHost.Open(server, logs);
    }

    /// <summary>Opens <paramref name="server" />. An in-process server uses <paramref name="keyStore" />. Null means no keys. A remote server ignores the key store.</summary>
    public static IFileManagerApi Open(FileManagerServer server, ILoggerFactory? logs, FileManagerKeyStore? keyStore)
    {
        ArgumentHelpers.ThrowIfNull(server);
        return server.Kind == FileManagerServerKind.Remote ? new FileStorageHttpApi(server) : FileStorageLocalHost.Open(server, keyStore, logs);
    }
}