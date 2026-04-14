using Lyo.FileMetadataStore.Postgres.Database;
using Lyo.FileStorage.Multipart;
using Microsoft.EntityFrameworkCore;

namespace Lyo.FileMetadataStore.Postgres;

public sealed class PostgresMultipartUploadSessionStore : IMultipartUploadSessionStore
{
    private readonly IDbContextFactory<FileMetadataStoreDbContext> _dbFactory;

    public PostgresMultipartUploadSessionStore(IDbContextFactory<FileMetadataStoreDbContext> dbFactory)
    {
        ArgumentNullException.ThrowIfNull(dbFactory);
        _dbFactory = dbFactory;
    }

    public async Task CreateAsync(MultipartUploadSessionRecord session, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        await using var db = await _dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        db.MultipartUploadSessions.Add(ToEntity(session));
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<MultipartUploadSessionRecord?> GetAsync(Guid sessionId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var e = await db.MultipartUploadSessions.AsNoTracking().FirstOrDefaultAsync(x => x.SessionId == sessionId, ct).ConfigureAwait(false);
        return e == null ? null : FromEntity(e);
    }

    public async Task UpdateProviderStateAsync(Guid sessionId, string providerStateJson, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var e = await db.MultipartUploadSessions.FirstOrDefaultAsync(x => x.SessionId == sessionId, ct).ConfigureAwait(false);
        if (e == null)
            return;

        e.ProviderState = providerStateJson;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SetStatusAsync(Guid sessionId, MultipartSessionStatus status, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var e = await db.MultipartUploadSessions.FirstOrDefaultAsync(x => x.SessionId == sessionId, ct).ConfigureAwait(false);
        if (e == null)
            return;

        e.Status = (int)status;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid sessionId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var e = await db.MultipartUploadSessions.FirstOrDefaultAsync(x => x.SessionId == sessionId, ct).ConfigureAwait(false);
        if (e == null)
            return;

        db.MultipartUploadSessions.Remove(e);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static MultipartUploadSessionEntity ToEntity(MultipartUploadSessionRecord s)
        => new() {
            SessionId = s.SessionId,
            TenantId = s.TenantId,
            CreatedUtc = s.CreatedUtc,
            ExpiresUtc = s.ExpiresUtc,
            TargetFileId = s.TargetFileId,
            PathPrefix = s.PathPrefix,
            Compress = s.Compress,
            Encrypt = s.Encrypt,
            KeyId = s.KeyId,
            OriginalFileName = s.OriginalFileName,
            ContentType = s.ContentType,
            Status = (int)s.Status,
            ProviderKind = (int)s.ProviderKind,
            ProviderState = s.ProviderStateJson,
            DeclaredContentLength = s.DeclaredContentLength,
            PartSizeBytes = s.PartSizeBytes
        };

    private static MultipartUploadSessionRecord FromEntity(MultipartUploadSessionEntity e)
        => new(
            e.SessionId, e.TenantId, e.CreatedUtc, e.ExpiresUtc, e.TargetFileId, e.PathPrefix, e.Compress, e.Encrypt, e.KeyId, e.OriginalFileName, e.ContentType,
            (MultipartSessionStatus)e.Status, (MultipartUploadProviderKind)e.ProviderKind, e.ProviderState, e.DeclaredContentLength, e.PartSizeBytes);
}