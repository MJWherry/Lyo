using Lyo.FileMetadataStore.Sqlite.Database;
using Lyo.FileStorage.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FileMetadataStore.Sqlite;

public sealed class SqliteFileAuditSink : IFileAuditEventHandler
{
    private readonly IDbContextFactory<SqliteFileMetadataStoreDbContext> _dbFactory;
    private readonly ILogger<SqliteFileAuditSink> _logger;

    public SqliteFileAuditSink(IDbContextFactory<SqliteFileMetadataStoreDbContext> dbFactory, ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(dbFactory);
        _dbFactory = dbFactory;
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<SqliteFileAuditSink>();
    }

    public Task HandleAsync(FileAuditEvent auditEvent, CancellationToken ct = default) => PersistAsync(auditEvent, ct);

    private async Task PersistAsync(FileAuditEvent auditEvent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        await using var db = await _dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = new FileAuditEventEntity {
            Id = Guid.NewGuid(),
            EventType = auditEvent.EventType.ToString(),
            Timestamp = auditEvent.UtcTimestamp.Kind == DateTimeKind.Utc ? auditEvent.UtcTimestamp : auditEvent.UtcTimestamp.ToUniversalTime(),
            FileId = auditEvent.FileId,
            TenantId = auditEvent.TenantId,
            ActorId = auditEvent.ActorId,
            DataEncryptionKeyId = auditEvent.DataEncryptionKeyId,
            DataEncryptionKeyVersion = auditEvent.DataEncryptionKeyVersion,
            Outcome = auditEvent.Outcome.ToString(),
            Error = auditEvent.Error,
            CorrelationId = auditEvent.CorrelationId
        };

        db.FileAuditEvents.Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogTrace("Appended file audit event {EventType} for file {FileId}", auditEvent.EventType, auditEvent.FileId);
    }
}