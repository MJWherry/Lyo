using Lyo.Exceptions;
using Lyo.FileMetadataStore.Postgres.Database;
using Lyo.FileStorage.Staged;
using Microsoft.EntityFrameworkCore;

namespace Lyo.FileMetadataStore.Postgres;

/// <summary>EF Core <see cref="IStagedFileUploadStore" /> backed by the <c>filestore.staged_file_upload</c> table.</summary>
public sealed class PostgresStagedFileUploadStore : IStagedFileUploadStore
{
    private readonly IDbContextFactory<FileMetadataStoreDbContext> _dbFactory;

    public PostgresStagedFileUploadStore(IDbContextFactory<FileMetadataStoreDbContext> dbFactory)
    {
        ArgumentNullException.ThrowIfNull(dbFactory);
        _dbFactory = dbFactory;
    }

    public async Task CreateAsync(StagedFileUploadRecord record, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        await using var db = await _dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        db.StagedFileUploads.Add(StagedFileUploadEntityMapping.ToEntity(record));
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<StagedFileUploadRecord?> GetAsync(Guid stageId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var e = await db.StagedFileUploads.AsNoTracking().FirstOrDefaultAsync(x => x.StageId == stageId, ct).ConfigureAwait(false);
        return e == null ? null : StagedFileUploadEntityMapping.FromEntity(e);
    }

    public async Task UpdateAsync(StagedFileUploadRecord record, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        await using var db = await _dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var e = await db.StagedFileUploads.FirstOrDefaultAsync(x => x.StageId == record.StageId, ct).ConfigureAwait(false);
        if (e == null)
            throw new NotFoundException($"Staged file upload {record.StageId} was not found.");

        var mapped = StagedFileUploadEntityMapping.ToEntity(record);
        e.TenantId = mapped.TenantId;
        e.OwnerId = mapped.OwnerId;
        e.CreatedUtc = mapped.CreatedUtc;
        e.ExpiresUtc = mapped.ExpiresUtc;
        e.Status = mapped.Status;
        e.StorageLocation = mapped.StorageLocation;
        e.PathPrefix = mapped.PathPrefix;
        e.OriginalFileName = mapped.OriginalFileName;
        e.ContentType = mapped.ContentType;
        e.DeclaredMaxSizeBytes = mapped.DeclaredMaxSizeBytes;
        e.ObservedSizeBytes = mapped.ObservedSizeBytes;
        e.ContentHash = mapped.ContentHash;
        e.HashAlgorithm = mapped.HashAlgorithm;
        e.ProviderKind = mapped.ProviderKind;
        e.ProviderState = mapped.ProviderState;
        e.CommittedFileId = mapped.CommittedFileId;
        e.FailureReason = mapped.FailureReason;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<bool> TryTransitionStatusAsync(Guid stageId, StagedUploadStatus from, StagedUploadStatus to, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var e = await db.StagedFileUploads.FirstOrDefaultAsync(x => x.StageId == stageId, ct).ConfigureAwait(false);
        if (e == null || e.Status != from)
            return false;

        e.Status = to;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }
}