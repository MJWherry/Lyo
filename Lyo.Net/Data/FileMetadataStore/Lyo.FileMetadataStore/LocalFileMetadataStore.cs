using System.Text.Json;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.FileMetadataStore.Models;
using Lyo.Hashing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FileMetadataStore;

/// <summary>File metadata store that writes each record as a JSON file on the local disk.</summary>
public class LocalFileMetadataStore : IFileMetadataStore
{
    private const string MetadataExtension = ".meta";
    private const string TagExtension = ".tags.json";
    private readonly ILogger<LocalFileMetadataStore> _logger;
    private readonly string _rootDirectoryPath;
    private readonly SemaphoreSlim _tagGate = new(1, 1);

    public LocalFileMetadataStore(string rootDirectoryPath, ILoggerFactory? loggerFactory = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(rootDirectoryPath);
        _rootDirectoryPath = rootDirectoryPath;
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<LocalFileMetadataStore>();
        if (Directory.Exists(_rootDirectoryPath))
            return;

        Directory.CreateDirectory(_rootDirectoryPath);
        _logger.LogInformation("Created root directory for metadata: {RootPath}", _rootDirectoryPath);
    }

    /// <inheritdoc />
    public async Task<FileStoreResult> GetMetadataAsync(Guid fileId, CancellationToken ct = default)
    {
        _logger.LogDebug("Retrieving metadata for file {FileId}", fileId);
        var metadataPath = GetMetadataPath(fileId);
        if (!File.Exists(metadataPath)) {
            _logger.LogWarning("Metadata file not found for {FileId}", fileId);
            ArgumentHelpers.ThrowIfFileNotFound(metadataPath);
        }

        try {
#if NETSTANDARD2_0
            ct.ThrowIfCancellationRequested();
            var json = File.ReadAllText(metadataPath);
#else
            var json = await File.ReadAllTextAsync(metadataPath, ct).ConfigureAwait(false);
#endif
            var metadata = JsonSerializer.Deserialize<FileStoreResult>(json);
            OperationHelpers.ThrowIfNull(metadata, $"Failed to deserialize metadata for file {fileId}");
            if (metadata.DeletedAt != null) {
                _logger.LogWarning("Metadata is soft-deleted for {FileId}", fileId);
                throw new FileNotFoundException($"Metadata for file {fileId} not found");
            }

            _logger.LogDebug("Retrieved metadata for file {FileId}", fileId);
            return metadata;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to read metadata for file {FileId}", fileId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SaveMetadataAsync(Guid fileId, FileStoreResult metadata, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(metadata);
        _logger.LogDebug("Saving metadata for file {FileId}", fileId);
        var metadataPath = GetMetadataPath(fileId);
        var directory = Path.GetDirectoryName(metadataPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        // Property names are an on-disk contract for existing .meta files, so this path does not use LyoJsonSerializerOptions.
        var options = new JsonSerializerOptions { WriteIndented = false };
        var json = JsonSerializer.Serialize(metadata, options);
#if NETSTANDARD2_0
        ct.ThrowIfCancellationRequested();
        File.WriteAllText(metadataPath, json);
#else
        await File.WriteAllTextAsync(metadataPath, json, ct).ConfigureAwait(false);
#endif
        _logger.LogDebug("Saved metadata for file {FileId}", fileId);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteMetadataAsync(Guid fileId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _logger.LogDebug("Deleting metadata for file {FileId}", fileId);
        var metadataPath = GetMetadataPath(fileId);
        if (!File.Exists(metadataPath)) {
            _logger.LogDebug("Metadata file not found for {FileId}, nothing to delete", fileId);
            return false;
        }

        try {
#if NETSTANDARD2_0
            ct.ThrowIfCancellationRequested();
            var json = File.ReadAllText(metadataPath);
#else
            var json = await File.ReadAllTextAsync(metadataPath, ct).ConfigureAwait(false);
#endif
            var metadata = JsonSerializer.Deserialize<FileStoreResult>(json);
            if (metadata == null) {
                _logger.LogWarning("Failed to deserialize metadata for deletion {FileId}", fileId);
                return false;
            }

            if (metadata.DeletedAt != null) {
                _logger.LogDebug("Metadata for {FileId} already soft-deleted", fileId);
                return false;
            }

            var options = new JsonSerializerOptions { WriteIndented = false };
            var tombstoned = metadata with { DeletedAt = DateTime.UtcNow, Availability = FileAvailability.Deleted };
            var serialized = JsonSerializer.Serialize(tombstoned, options);
#if NETSTANDARD2_0
            File.WriteAllText(metadataPath, serialized);
#else
            await File.WriteAllTextAsync(metadataPath, serialized, ct).ConfigureAwait(false);
#endif
            _logger.LogDebug("Soft-deleted metadata file for {FileId}", fileId);
            return true;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to soft-delete metadata for file {FileId}", fileId);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<bool> PurgeMetadataAsync(Guid fileId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _logger.LogDebug("Purging metadata file for file {FileId}", fileId);
        var metadataPath = GetMetadataPath(fileId);
        if (!File.Exists(metadataPath)) {
            _logger.LogDebug("Metadata file not found for {FileId}, nothing to purge", fileId);
            return Task.FromResult(false);
        }

        try {
            File.Delete(metadataPath);
            var tagPath = TagPath(fileId);
            if (File.Exists(tagPath))
                File.Delete(tagPath);

            _logger.LogDebug("Purged metadata file for file {FileId}", fileId);
            return Task.FromResult(true);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to purge metadata for file {FileId}", fileId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<FileStoreResult?> FindByHashAsync(byte[] hash, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(hash);
        _logger.LogDebug("Searching for metadata by hash");

        // JSON-on-disk must scan every metadata file for hash matches.
        // That is slow; prefer the SQLite store when duplicate detection is required.
        if (!Directory.Exists(_rootDirectoryPath))
            return null;

        try {
            var metadataFiles = Directory.GetFiles(_rootDirectoryPath, $"*{MetadataExtension}", SearchOption.AllDirectories);
            foreach (var metadataPath in metadataFiles) {
                ct.ThrowIfCancellationRequested();
                try {
#if NETSTANDARD2_0
                    var json = File.ReadAllText(metadataPath);
#else
                    var json = await File.ReadAllTextAsync(metadataPath, ct).ConfigureAwait(false);
#endif
                    var metadata = JsonSerializer.Deserialize<FileStoreResult>(json);
                    if (metadata?.DeletedAt != null || metadata?.Availability == FileAvailability.PendingDirectUpload ||
                        metadata?.OriginalFileHash is not { Length: > 0 } storedHash || !HashingService.Shared.FixedTimeEquals(storedHash.AsSpan(), hash.AsSpan()))
                        continue;

                    _logger.LogDebug("Found metadata by hash for file {FileId}", metadata.Id);
                    return metadata;
                }
                catch (Exception ex) {
                    _logger.LogWarning(ex, "Failed to read metadata file {MetadataPath}", metadataPath);
                    // Keep scanning remaining files
                }
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error searching for metadata by hash");
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<FileStoreResult>> FindByKeyIdAndVersionAsync(string keyId, string? keyVersion = null, CancellationToken ct = default)
    {
        _logger.LogDebug("Searching for metadata by keyId '{KeyId}' and version {KeyVersion}", keyId, keyVersion ?? "any");
        var results = new List<FileStoreResult>();
        if (!Directory.Exists(_rootDirectoryPath))
            return results;

        try {
            var metadataFiles = Directory.GetFiles(_rootDirectoryPath, $"*{MetadataExtension}", SearchOption.AllDirectories);
            foreach (var metadataPath in metadataFiles) {
                ct.ThrowIfCancellationRequested();
                try {
#if NETSTANDARD2_0
                    var json = File.ReadAllText(metadataPath);
#else
                    var json = await File.ReadAllTextAsync(metadataPath, ct).ConfigureAwait(false);
#endif
                    var metadata = JsonSerializer.Deserialize<FileStoreResult>(json);
                    if (metadata != null && metadata.DeletedAt == null && metadata.IsEncrypted && metadata.DataEncryptionKeyId == keyId &&
                        (keyVersion == null || metadata.DataEncryptionKeyVersion == keyVersion))
                        results.Add(metadata);
                }
                catch (Exception ex) {
                    _logger.LogWarning(ex, "Failed to read metadata file {MetadataPath}", metadataPath);
                    // Keep scanning remaining files
                }
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error searching for metadata by keyId and version");
        }

        _logger.LogDebug("Found {Count} files matching keyId '{KeyId}' and version {KeyVersion}", results.Count, keyId, keyVersion ?? "any");
        return results;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileStoreResult>> ListByPathPrefixAsync(string? pathPrefix, bool includeDescendants, int maxKeys, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfLessThan(maxKeys, 1);
        var results = new List<FileStoreResult>();
        if (!Directory.Exists(_rootDirectoryPath))
            return results;

        var metadataFiles = Directory.GetFiles(_rootDirectoryPath, $"*{MetadataExtension}", SearchOption.AllDirectories);
        foreach (var metadataPath in metadataFiles) {
            ct.ThrowIfCancellationRequested();
            try {
#if NETSTANDARD2_0
                var json = File.ReadAllText(metadataPath);
#else
                var json = await File.ReadAllTextAsync(metadataPath, ct).ConfigureAwait(false);
#endif
                var metadata = JsonSerializer.Deserialize<FileStoreResult>(json);
                if (metadata == null || metadata.DeletedAt != null)
                    continue;

                if (!FileMetadataPathPrefix.IsUnder(pathPrefix, metadata.PathPrefix, includeDescendants))
                    continue;

                results.Add(metadata);
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "Failed to read metadata file {MetadataPath}", metadataPath);
            }
        }

        return results.OrderBy(static r => r.PathPrefix, StringComparer.Ordinal).ThenBy(static r => r.Id).Take(maxKeys).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileStoreResult>> ListPendingDirectUploadsAsync(DateTime beganBefore, int maxKeys, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfLessThan(maxKeys, 1);
        var results = new List<FileStoreResult>();
        if (!Directory.Exists(_rootDirectoryPath))
            return results;

        var cutoff = DateTime.SpecifyKind(beganBefore, DateTimeKind.Utc);
        foreach (var metadataPath in Directory.GetFiles(_rootDirectoryPath, $"*{MetadataExtension}", SearchOption.AllDirectories)) {
            ct.ThrowIfCancellationRequested();
            try {
#if NETSTANDARD2_0
                var json = File.ReadAllText(metadataPath);
#else
                var json = await File.ReadAllTextAsync(metadataPath, ct).ConfigureAwait(false);
#endif
                var metadata = JsonSerializer.Deserialize<FileStoreResult>(json);
                if (metadata is { DeletedAt: null, Availability: FileAvailability.PendingDirectUpload } && metadata.Timestamp.ToUniversalTime() < cutoff)
                    results.Add(metadata);
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "Failed to read metadata file {MetadataPath}", metadataPath);
            }
        }

        return results.OrderBy(static r => r.Timestamp).Take(maxKeys).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileStoreResult>> SearchByNameAsync(string text, int maxKeys, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(text);
        ArgumentHelpers.ThrowIfLessThan(maxKeys, 1);
        var results = new List<FileStoreResult>();
        if (!Directory.Exists(_rootDirectoryPath))
            return results;

        foreach (var metadataPath in Directory.GetFiles(_rootDirectoryPath, $"*{MetadataExtension}", SearchOption.AllDirectories)) {
            ct.ThrowIfCancellationRequested();
            try {
#if NETSTANDARD2_0
                var json = File.ReadAllText(metadataPath);
#else
                var json = await File.ReadAllTextAsync(metadataPath, ct).ConfigureAwait(false);
#endif
                var metadata = JsonSerializer.Deserialize<FileStoreResult>(json);
                if (metadata is { DeletedAt: null, OriginalFileName: { } name } && name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                    results.Add(metadata);
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "Failed to read metadata file {MetadataPath}", metadataPath);
            }
        }

        return results.OrderBy(static row => row.OriginalFileName, StringComparer.OrdinalIgnoreCase).Take(maxKeys).ToList();
    }

    /// <inheritdoc />
    public async Task<FileRevision> SaveNewVersionAsync(Guid fileId, string storedKey, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(storedKey);
        var current = await GetMetadataAsync(fileId, ct).ConfigureAwait(false);
        var revision = new FileRevision(Guid.NewGuid(), fileId, current.SourceFileName, DateTime.UtcNow);
        var directory = Path.Combine(_rootDirectoryPath, "versions");
        Directory.CreateDirectory(directory);
        var json = JsonSerializer.Serialize(revision);
        var path = Path.Combine(directory, revision.Id.ToString("N") + ".ver");
#if NETSTANDARD2_0
        ct.ThrowIfCancellationRequested();
        File.WriteAllText(path, json);
#else
        await File.WriteAllTextAsync(path, json, ct).ConfigureAwait(false);
#endif
        await SaveMetadataAsync(fileId, current with { SourceFileName = storedKey }, ct).ConfigureAwait(false);
        return revision;
    }

    /// <inheritdoc />
    public async Task<FileRevision?> GetVersionAsync(Guid revisionId, CancellationToken ct = default)
    {
        var path = Path.Combine(_rootDirectoryPath, "versions", revisionId.ToString("N") + ".ver");
        if (!File.Exists(path))
            return null;
#if NETSTANDARD2_0
        ct.ThrowIfCancellationRequested();
        var json = File.ReadAllText(path);
#else
        var json = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
#endif
        return JsonSerializer.Deserialize<FileRevision>(json);
    }

    /// <inheritdoc />
    public async Task<FileTag> AddFileTagAsync(Guid fileId, string tagType, string value, string? slug, bool automatic, CancellationToken ct = default)
    {
        FileTagTypes.Validate(tagType, value, slug);
        await _tagGate.WaitAsync(ct).ConfigureAwait(false);
        try {
            var tags = await ReadTagsAsync(fileId, ct).ConfigureAwait(false);
            foreach (var tag in tags) {
                if (tag.TagType == tagType && tag.Value == value)
                    return tag;
            }

            var created = new FileTag(LyoGuid.CreateV7(), fileId, tagType, value, slug ?? "", automatic);
            tags.Add(created);
            await WriteTagsAsync(fileId, tags, ct).ConfigureAwait(false);
            return created;
        }
        finally {
            _tagGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> RemoveFileTagAsync(Guid fileId, string tagType, string value, CancellationToken ct = default)
    {
        await _tagGate.WaitAsync(ct).ConfigureAwait(false);
        try {
            var tags = await ReadTagsAsync(fileId, ct).ConfigureAwait(false);
            var removed = tags.RemoveAll(t => t.TagType == tagType && t.Value == value);
            if (removed == 0)
                return false;

            await WriteTagsAsync(fileId, tags, ct).ConfigureAwait(false);
            return true;
        }
        finally {
            _tagGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileTag>> ListFileTagsAsync(Guid fileId, CancellationToken ct = default)
    {
        await _tagGate.WaitAsync(ct).ConfigureAwait(false);
        try {
            return await ReadTagsAsync(fileId, ct).ConfigureAwait(false);
        }
        finally {
            _tagGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task CopyFileTagsAsync(Guid sourceFileId, Guid destinationFileId, CancellationToken ct = default)
    {
        var source = await ListFileTagsAsync(sourceFileId, ct).ConfigureAwait(false);
        foreach (var tag in source)
            await AddFileTagAsync(destinationFileId, tag.TagType, tag.Value, tag.Slug, tag.Automatic, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteFileTagsAsync(Guid fileId, CancellationToken ct = default)
    {
        await _tagGate.WaitAsync(ct).ConfigureAwait(false);
        try {
            var path = TagPath(fileId);
            if (File.Exists(path))
                File.Delete(path);
        }
        finally {
            _tagGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ListManualTagTypesAsync(CancellationToken ct = default)
    {
        var files = await ListByPathPrefixAsync(null, true, int.MaxValue, ct).ConfigureAwait(false);
        var live = new HashSet<Guid>();
        foreach (var file in files) {
            if (file.DeletedAt == null)
                live.Add(file.Id);
        }

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var tag in await ReadAllTagsAsync(ct).ConfigureAwait(false)) {
            if (!tag.Automatic && live.Contains(tag.FileId))
                names.Add(tag.TagType);
        }

        return names.ToArray();
    }

    /// <inheritdoc />
    public async Task<FileTagGroupPage> ListTagGroupAsync(IReadOnlyList<FileTagGroupKey> groupBy, string? virtualPath, int maxKeys, CancellationToken ct = default)
    {
        var files = await ListByPathPrefixAsync(null, true, int.MaxValue, ct).ConfigureAwait(false);
        var tags = await ReadAllTagsAsync(ct).ConfigureAwait(false);
        return FileTagGrouper.Group(groupBy, virtualPath, files, tags, maxKeys);
    }

    private async Task<List<FileTag>> ReadAllTagsAsync(CancellationToken ct)
    {
        if (!Directory.Exists(_rootDirectoryPath))
            return [];

        var all = new List<FileTag>();
        foreach (var path in Directory.GetFiles(_rootDirectoryPath, "*" + TagExtension, SearchOption.AllDirectories)) {
            ct.ThrowIfCancellationRequested();
            var json = await ReadTagTextAsync(path, ct).ConfigureAwait(false);
            var tags = JsonSerializer.Deserialize<List<FileTag>>(json);
            if (tags != null)
                all.AddRange(tags);
        }

        return all;
    }

    private async Task<List<FileTag>> ReadTagsAsync(Guid fileId, CancellationToken ct)
    {
        var path = TagPath(fileId);
        if (!File.Exists(path))
            return [];

        var json = await ReadTagTextAsync(path, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<List<FileTag>>(json) ?? [];
    }

    private async Task WriteTagsAsync(Guid fileId, List<FileTag> tags, CancellationToken ct)
    {
        var path = TagPath(fileId);
        if (tags.Count == 0) {
            if (File.Exists(path))
                File.Delete(path);

            return;
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(tags);
#if NETSTANDARD2_0
        ct.ThrowIfCancellationRequested();
        File.WriteAllText(path, json);
        await Task.CompletedTask.ConfigureAwait(false);
#else
        await File.WriteAllTextAsync(path, json, ct).ConfigureAwait(false);
#endif
    }

    private static async Task<string> ReadTagTextAsync(string path, CancellationToken ct)
    {
#if NETSTANDARD2_0
        ct.ThrowIfCancellationRequested();
        return File.ReadAllText(path);
#else
        return await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
#endif
    }

    private string TagPath(Guid fileId)
    {
        var idString = fileId.ToString("N");
        var subDir = Path.Combine(idString.Substring(0, 2), idString.Substring(2, 2));
        return Path.Combine(_rootDirectoryPath, subDir, idString + TagExtension);
    }

    private string GetMetadataPath(Guid fileId)
    {
        var idString = fileId.ToString("N");
        var subDir = Path.Combine(idString.Substring(0, 2), idString.Substring(2, 2));
        var fileName = fileId.ToString("N") + MetadataExtension;
        return Path.Combine(_rootDirectoryPath, subDir, fileName);
    }
}