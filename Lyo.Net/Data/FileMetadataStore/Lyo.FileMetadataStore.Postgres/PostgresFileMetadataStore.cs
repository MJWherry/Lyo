using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.FileMetadataStore.Models;
using Lyo.FileMetadataStore.Postgres.Database;
using Lyo.Health;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FileMetadataStore.Postgres;

/// <summary>
/// File metadata store that stores metadata in a PostgreSQL database using Entity Framework Core. Provides better performance and querying capabilities compared to JSON
/// file-based storage.
/// </summary>
public class PostgresFileMetadataStore : IFileMetadataStore, IHealth, IDisposable
{
    private readonly FileMetadataStoreDbContext _dbContext;
    private readonly ILogger<PostgresFileMetadataStore> _logger;
    private bool _disposed;

    public PostgresFileMetadataStore(FileMetadataStoreDbContext dbContext, ILoggerFactory? loggerFactory = null)
    {
        ArgumentHelpers.ThrowIfNull(dbContext, nameof(dbContext));
        _dbContext = dbContext;
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<PostgresFileMetadataStore>();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed) {
            _dbContext?.Dispose();
            _disposed = true;
        }
    }

    /// <inheritdoc />
    public async Task<FileStoreResult> GetMetadataAsync(Guid fileId, CancellationToken ct = default)
    {
        _logger.LogDebug("Retrieving metadata for file {FileId}", fileId);
        var entity = await _dbContext.FileMetadata.FirstOrDefaultAsync(e => e.Id == fileId.ToString(), ct).ConfigureAwait(false);
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
        ArgumentHelpers.ThrowIfNull(metadata, nameof(metadata));
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

        _dbContext.FileMetadata.Remove(entity);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogDebug("Deleted metadata from database for file {FileId}", fileId);
        return true;
    }

    /// <inheritdoc />
    public async Task<FileStoreResult?> FindByHashAsync(byte[] hash, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(hash, nameof(hash));
        _logger.LogDebug("Searching for metadata by hash");

        // EF Core doesn't support direct byte array comparison efficiently,
        // so we need to load entities and compare in memory
        // For better performance, consider using a hash index or computed column
        var entities = await _dbContext.FileMetadata.Where(e => e.OriginalFileHash != null && e.OriginalFileHash.Length == hash.Length).ToListAsync(ct).ConfigureAwait(false);
        var entity = entities.FirstOrDefault(e => ByteArraysEqual(e.OriginalFileHash, hash));
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
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId, nameof(keyId));
        _logger.LogDebug("Searching for metadata by keyId '{KeyId}' and version {KeyVersion}", keyId, keyVersion ?? "any");
        var query = _dbContext.FileMetadata.Where(e => e.DataEncryptionKeyId == keyId && e.IsEncrypted);
        if (keyVersion != null)
            query = query.Where(e => e.DataEncryptionKeyVersion == keyVersion);

        var entities = await query.ToListAsync(ct).ConfigureAwait(false);
        var results = entities.Select(e => e.ToFileStoreResult()).ToList();
        _logger.LogDebug("Found {Count} files matching keyId '{KeyId}' and version {KeyVersion}", results.Count, keyId, keyVersion ?? "any");
        return results;
    }

    /// <inheritdoc />
    public string HealthCheckName => "filestore-postgres";

    /// <inheritdoc />
    public async Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try {
            var canConnect = await _dbContext.Database.CanConnectAsync(ct).ConfigureAwait(false);
            sw.Stop();
            return canConnect
                ? HealthResult.Healthy(sw.Elapsed, null, new Dictionary<string, object?> { ["database"] = "filestore" })
                : HealthResult.Unhealthy(sw.Elapsed, "Database connection failed");
        }
        catch (Exception ex) {
            sw.Stop();
            return HealthResult.Unhealthy(sw.Elapsed, ex.Message, null, ex);
        }
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