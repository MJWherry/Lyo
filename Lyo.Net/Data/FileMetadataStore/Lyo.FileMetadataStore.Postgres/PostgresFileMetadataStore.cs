using Lyo.Exceptions;
using Lyo.FileMetadataStore;
using Lyo.FileMetadataStore.Models;
using Lyo.FileMetadataStore.Postgres.Database;
using Lyo.Health;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FileMetadataStore.Postgres;

/// <summary>
/// File metadata store backed by PostgreSQL via Entity Framework Core. Faster to query than JSON-on-disk storage.
/// </summary>
public class PostgresFileMetadataStore : IFileMetadataStore, IHealth, IDisposable
{
    private readonly FileMetadataStoreDbContext _dbContext;
    private readonly ILogger<PostgresFileMetadataStore> _logger;
    private bool _disposed;

    public PostgresFileMetadataStore(FileMetadataStoreDbContext dbContext, ILoggerFactory? loggerFactory = null)
    {
        ArgumentHelpers.ThrowIfNull(dbContext);
        _dbContext = dbContext;
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<PostgresFileMetadataStore>();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _dbContext.Dispose();
        _disposed = true;
    }

    /// <inheritdoc />
    public async Task<FileStoreResult> GetMetadataAsync(Guid fileId, CancellationToken ct = default)
    {
        _logger.LogDebug("Retrieving metadata for file {FileId}", fileId);
        var entity = await _dbContext.FileMetadata.FirstOrDefaultAsync(e => e.Id == fileId.ToString() && e.DeletedAt == null, ct).ConfigureAwait(false);
        if (entity == null) {
            _logger.LogWarning("Metadata not found in database for {FileId}", fileId);
            throw new FileNotFoundException($"Metadata for file {fileId} not found");
        }

        var metadata = entity.ToFileStoreResult();
        _logger.LogDebug("Retrieved metadata for file {FileId}", fileId);
        return metadata;
    }

    /// <inheritdoc />
    public async Task SaveMetadataAsync(Guid fileId, FileStoreResult metadata, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(metadata);
        _logger.LogDebug("Saving metadata for file {FileId}", fileId);
        var entity = FileMetadataEntity.FromFileStoreResult(metadata);
        var existing = await _dbContext.FileMetadata.FirstOrDefaultAsync(e => e.Id == fileId.ToString(), ct).ConfigureAwait(false);
        if (existing != null)
            _dbContext.Entry(existing).CurrentValues.SetValues(entity);
        else
            _dbContext.FileMetadata.Add(entity);

        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogDebug("Saved metadata to PostgreSQL database for file {FileId}", fileId);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteMetadataAsync(Guid fileId, CancellationToken ct = default)
    {
        _logger.LogDebug("Deleting metadata for file {FileId}", fileId);
        var entity = await _dbContext.FileMetadata.FirstOrDefaultAsync(e => e.Id == fileId.ToString(), ct).ConfigureAwait(false);
        if (entity == null) {
            _logger.LogDebug("Metadata not found in database for {FileId}, nothing to delete", fileId);
            return false;
        }

        if (entity.DeletedAt != null) {
            _logger.LogDebug("Metadata for {FileId} already soft-deleted", fileId);
            return false;
        }

        entity.MarkDeleted();
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogDebug("Soft-deleted metadata in database for file {FileId}", fileId);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> PurgeMetadataAsync(Guid fileId, CancellationToken ct = default)
    {
        _logger.LogDebug("Purging metadata for file {FileId}", fileId);
        var entity = await _dbContext.FileMetadata.FirstOrDefaultAsync(e => e.Id == fileId.ToString(), ct).ConfigureAwait(false);
        if (entity == null) {
            _logger.LogDebug("Metadata not found in database for {FileId}, nothing to purge", fileId);
            return false;
        }

        _dbContext.FileMetadata.Remove(entity);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogDebug("Purged metadata row for file {FileId}", fileId);
        return true;
    }

    /// <inheritdoc />
    public async Task<FileStoreResult?> FindByHashAsync(byte[] hash, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(hash);
        _logger.LogDebug("Searching for metadata by hash");

        // Let PostgreSQL evaluate bytea equality so the hash index can be used.
        var entity = await _dbContext.FileMetadata.FirstOrDefaultAsync(
                e => e.OriginalFileHash == hash && e.DeletedAt == null && (e.Availability == null || e.Availability != nameof(FileAvailability.PendingDirectUpload)), ct)
            .ConfigureAwait(false);

        if (entity == null) {
            _logger.LogDebug("No metadata found for hash");
            return null;
        }

        var metadata = entity.ToFileStoreResult();
        _logger.LogDebug("Found metadata by hash for file {FileId}", metadata.Id);
        return metadata;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<FileStoreResult>> FindByKeyIdAndVersionAsync(string keyId, string? keyVersion = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId);
        _logger.LogDebug("Searching for metadata by keyId '{KeyId}' and version {KeyVersion}", keyId, keyVersion ?? "any");
        var query = _dbContext.FileMetadata.Where(e => e.DataEncryptionKeyId == keyId && e.IsEncrypted && e.DeletedAt == null);
        if (keyVersion != null)
            query = query.Where(e => e.DataEncryptionKeyVersion == keyVersion);

        var entities = await query.ToListAsync(ct).ConfigureAwait(false);
        var results = entities.Select(e => e.ToFileStoreResult()).ToList();
        _logger.LogDebug("Found {Count} files matching keyId '{KeyId}' and version {KeyVersion}", results.Count, keyId, keyVersion ?? "any");
        return results;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileStoreResult>> ListByPathPrefixAsync(string? pathPrefix, bool includeDescendants, int maxKeys, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfLessThan(maxKeys, 1);
        var normalized = FileMetadataPathPrefix.Normalize(pathPrefix);
        var query = _dbContext.FileMetadata.Where(e => e.DeletedAt == null);
        if (!includeDescendants) {
            if (normalized == null)
                query = query.Where(e => e.PathPrefix == null || e.PathPrefix == "");
            else
                query = query.Where(e => e.PathPrefix == normalized);
        }
        else if (normalized != null) {
            var nested = normalized + "/";
            query = query.Where(e => e.PathPrefix == normalized || e.PathPrefix != null && e.PathPrefix.StartsWith(nested));
        }

        var entities = await query.OrderBy(e => e.PathPrefix).ThenBy(e => e.Id).Take(maxKeys).ToListAsync(ct).ConfigureAwait(false);
        return entities.Select(e => e.ToFileStoreResult()).ToList();
    }

    /// <inheritdoc />
    public string HealthCheckName => "filestore-postgres";

    /// <inheritdoc />
    public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
        => PostgresHealth.CheckAsync(_dbContext, PostgresFileMetadataStoreOptions.Schema, ct);
}