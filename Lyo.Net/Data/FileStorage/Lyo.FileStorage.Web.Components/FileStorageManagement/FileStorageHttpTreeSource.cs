using Lyo.Api.Client;
using Lyo.Api.FileStorage.Models;
using Lyo.Exceptions;
using Lyo.IO.FileSystem;

namespace Lyo.FileStorage.Web.Components.FileStorageManagement;

/// <summary>Lists FileStorage folder children over HTTP. Does not use QueryProject.</summary>
public sealed class FileStorageHttpTreeSource : IFileTreeSource
{
    private readonly IApiClient _api;
    private readonly Func<string, string> _filesApi;

    /// <summary>Client-only folder prefixes created in the inspector before a file is uploaded.</summary>
    public HashSet<string> PendingPrefixes { get; } = new(StringComparer.Ordinal);

    /// <summary>True when the last list was truncated.</summary>
    public bool Truncated { get; private set; }

    /// <summary>Builds a source against <paramref name="api" />. <paramref name="filesApi" /> maps a relative path such as <c>files/folder</c> to the host route.</summary>
    public FileStorageHttpTreeSource(IApiClient api, Func<string, string> filesApi)
    {
        ArgumentHelpers.ThrowIfNull(api);
        ArgumentHelpers.ThrowIfNull(filesApi);
        _api = api;
        _filesApi = filesApi;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileSystemEntry>> ListChildrenAsync(string path, CancellationToken ct = default)
    {
        var prefix = PrefixFromPath(path);
        var query = "files/folder?maxKeys=10000";
        if (prefix != null)
            query += "&pathPrefix=" + Uri.EscapeDataString(prefix);

        var listed = await _api.GetAsAsync<FileStorageFolderListResponse>(_filesApi(query), ct: ct).ConfigureAwait(false);
        Truncated = listed?.Truncated == true;
        List<FileSystemEntry> entries = [];
        foreach (var row in listed?.Entries ?? [])
            entries.Add(ToEntry(row));

        foreach (var pending in PendingPrefixes) {
            if (!string.Equals(ParentPrefix(pending), prefix, StringComparison.Ordinal))
                continue;

            var name = FileStoragePathTreeBuilder.SplitSegments(pending)[^1];
            if (entries.Any(e => e.IsDirectory && string.Equals(e.Name, name, StringComparison.Ordinal)))
                continue;

            entries.Add(
                new(
                    "/" + pending, name, true, 0, DateTimeOffset.MinValue, DateTimeOffset.MinValue,
                    new Dictionary<string, string> { ["Presence"] = nameof(FileStoragePresence.None), ["Pending"] = "true" }));
        }

        return entries;
    }

    /// <summary>Directory VFS path for a catalog PathPrefix. Root is <c>/</c>.</summary>
    public static string DirectoryVfsPath(string? pathPrefix)
        => pathPrefix == null ? "/" : "/" + pathPrefix;

    /// <summary>Inspector node for a catalog PathPrefix. Does not walk a tree <c>Root</c>.</summary>
    public static FileStoragePathTreeNode DirectoryNode(string? pathPrefix)
    {
        var path = DirectoryVfsPath(pathPrefix);
        var name = pathPrefix == null
            ? FileStoragePathTreeBuilder.RootDisplayName
            : FileStoragePathTreeBuilder.SplitSegments(pathPrefix)[^1];
        return ToPathNode(new(path, name, true, 0, DateTimeOffset.MinValue, DateTimeOffset.MinValue));
    }

    /// <summary>Maps a tree path (<c>/</c> or <c>/{prefix}</c>) to a catalog PathPrefix.</summary>
    public static string? PrefixFromPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path == "/")
            return null;

        return path.Trim().Trim('/');
    }

    /// <summary>Maps a folder-list DTO to a <see cref="FileSystemEntry" />.</summary>
    public static FileSystemEntry ToEntry(FileStorageFolderEntryDto row)
    {
        ArgumentHelpers.ThrowIfNull(row);
        var path = row.IsDirectory
            ? (row.PathPrefix == null ? "/" : "/" + row.PathPrefix)
            : FileStorageLogicalPathFor(row.PathPrefix, row.FileId, row.PhysicalKey);
        var props = new Dictionary<string, string> { ["Presence"] = row.Presence.ToString() };
        if (!row.IsDirectory) {
            if (row.FileId is { } id)
                props["FileId"] = id.ToString("D");
            if (!string.IsNullOrWhiteSpace(row.OriginalFileName))
                props["OriginalFileName"] = row.OriginalFileName;
            if (!string.IsNullOrWhiteSpace(row.PhysicalKey))
                props["PhysicalKey"] = row.PhysicalKey;
        }

        return new(path, row.Name, row.IsDirectory, row.OriginalFileSize, DateTimeOffset.MinValue, DateTimeOffset.MinValue, props);
    }

    /// <summary>Builds a <see cref="FileStoragePathTreeNode" /> for the inspector from a tree entry.</summary>
    public static FileStoragePathTreeNode ToPathNode(FileSystemEntry entry)
    {
        ArgumentHelpers.ThrowIfNull(entry);
        var presenceText = Prop(entry, "Presence");
        var presence = Enum.TryParse<FileStoragePresence>(presenceText, out var parsed)
            ? parsed
            : FileStoragePresence.None;
        Guid? fileId = null;
        if (Guid.TryParse(Prop(entry, "FileId"), out var parsedId) && parsedId != Guid.Empty)
            fileId = parsedId;

        var prefix = PrefixFromPath(entry.IsDirectory ? entry.Path : ParentPath(entry.Path));
        var physicalKey = Prop(entry, "PhysicalKey");
        return new() {
            Key = entry.IsDirectory ? FileStoragePathTreeBuilder.DirectoryKey(prefix) : "file:" + (fileId?.ToString("D") ?? physicalKey ?? entry.Path),
            Name = entry.Name,
            IsDirectory = entry.IsDirectory,
            PathPrefix = prefix,
            FileId = fileId,
            Presence = presence,
            PhysicalKey = physicalKey,
            SourceFileName = Prop(entry, "OriginalFileName"),
            OriginalFileSize = entry.IsDirectory ? null : entry.Length,
            IsPending = string.Equals(Prop(entry, "Pending"), "true", StringComparison.Ordinal)
        };
    }

    private static string FileStorageLogicalPathFor(string? pathPrefix, Guid? fileId, string? physicalKey)
    {
        if (fileId is { } id)
            return pathPrefix == null ? "/" + id.ToString("D") : "/" + pathPrefix + "/" + id.ToString("D");

        if (!string.IsNullOrWhiteSpace(physicalKey))
            return "/" + physicalKey.Replace('\\', '/').Trim('/');

        return pathPrefix == null ? "/" : "/" + pathPrefix;
    }

    private static string ParentPath(string path)
    {
        var trimmed = path.TrimEnd('/');
        var slash = trimmed.LastIndexOf('/');
        return slash <= 0 ? "/" : trimmed[..slash];
    }

    private static string? ParentPrefix(string prefix)
    {
        var slash = prefix.LastIndexOf('/');
        return slash < 0 ? null : prefix[..slash];
    }

    private static string? Prop(FileSystemEntry entry, string key)
    {
        if (entry.Properties == null || !entry.Properties.TryGetValue(key, out var value))
            return null;

        return value;
    }
}
