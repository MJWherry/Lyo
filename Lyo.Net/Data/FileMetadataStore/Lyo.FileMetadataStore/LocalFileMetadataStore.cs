using System.Text.Json;
using Lyo.Exceptions;
using Lyo.FileMetadataStore.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FileMetadataStore;

/// <summary>File metadata store that stores metadata as JSON files on the local filesystem.</summary>
public class LocalFileMetadataStore : IFileMetadataStore
{
    private const string MetadataExtension = ".meta";
    private readonly ILogger<LocalFileMetadataStore> _logger;
    private readonly string _rootDirectoryPath;

    public LocalFileMetadataStore(string rootDirectoryPath, ILoggerFactory? loggerFactory = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(rootDirectoryPath, nameof(rootDirectoryPath));
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
            ArgumentHelpers.ThrowIfFileNotFound(metadataPath, nameof(metadataPath));
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
        ArgumentHelpers.ThrowIfNull(metadata, nameof(metadata));
        _logger.LogDebug("Saving metadata for file {FileId}", fileId);
        var metadataPath = GetMetadataPath(fileId);
        var directory = Path.GetDirectoryName(metadataPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

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

        File.Delete(metadataPath);
        _logger.LogDebug("Deleted metadata file for {FileId}", fileId);
        return await Task.FromResult(true).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FileStoreResult?> FindByHashAsync(byte[] hash, CancellationToken ct = default)
    {
        _logger.LogDebug("Searching for metadata by hash");

        // For JSON file-based storage, we need to scan all metadata files
        // This is inefficient but necessary for duplicate detection
        // In practice, SQLite-based metadata store should be used if duplicate detection is needed
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
                    if (metadata != null && ByteArraysEqual(metadata.OriginalFileHash, hash)) {
                        _logger.LogDebug("Found metadata by hash for file {FileId}", metadata.Id);
                        return metadata;
                    }
                }
                catch (Exception ex) {
                    _logger.LogWarning(ex, "Failed to read metadata file {MetadataPath}", metadataPath);
                    // Continue searching other files
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
        if (!Directory.Exists(_rootDirectoryPath)) {
            _logger.LogDebug("Root directory does not exist, returning empty results");
            return results;
        }

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
                    if (metadata != null && metadata.IsEncrypted && metadata.DataEncryptionKeyId == keyId &&
                        (keyVersion == null || metadata.DataEncryptionKeyVersion == keyVersion))
                        results.Add(metadata);
                }
                catch (Exception ex) {
                    _logger.LogWarning(ex, "Failed to read metadata file {MetadataPath}", metadataPath);
                    // Continue searching other files
                }
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error searching for metadata by keyId and version");
        }

        _logger.LogDebug("Found {Count} files matching keyId '{KeyId}' and version {KeyVersion}", results.Count, keyId, keyVersion ?? "any");
        return results;
    }

    private string GetMetadataPath(Guid fileId)
    {
        var idString = fileId.ToString("N");
        var subDir = Path.Combine(idString.Substring(0, 2), idString.Substring(2, 2));
        var fileName = fileId.ToString("N") + MetadataExtension;
        return Path.Combine(_rootDirectoryPath, subDir, fileName);
    }

    private static bool ByteArraysEqual(byte[] a, byte[] b)
    {
        if (a == null || b == null)
            return a == b;

        if (a.Length != b.Length)
            return false;

        for (var i = 0; i < a.Length; i++) {
            if (a[i] != b[i])
                return false;
        }

        return true;
    }
}