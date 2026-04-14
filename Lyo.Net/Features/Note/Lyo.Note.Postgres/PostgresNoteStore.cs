using System.Diagnostics;
using Lyo.Common;
using Lyo.Exceptions;
using Lyo.Health;
using Lyo.Note.Postgres.Database;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Note.Postgres;

/// <summary>PostgreSQL implementation of INoteStore.</summary>
public sealed class PostgresNoteStore : INoteStore, IHealth
{
    private readonly IDbContextFactory<NoteDbContext> _contextFactory;

    /// <summary>Creates a new PostgresNoteStore.</summary>
    public PostgresNoteStore(IDbContextFactory<NoteDbContext> contextFactory)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory, nameof(contextFactory));
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public string HealthCheckName => "note-postgres";

    /// <inheritdoc />
    public async Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try {
            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var canConnect = await context.Database.CanConnectAsync(ct).ConfigureAwait(false);
            sw.Stop();
            return canConnect
                ? HealthResult.Healthy(sw.Elapsed, null, new Dictionary<string, object?> { ["database"] = "note" })
                : HealthResult.Unhealthy(sw.Elapsed, "Database connection failed");
        }
        catch (Exception ex) {
            sw.Stop();
            return HealthResult.Unhealthy(sw.Elapsed, ex.Message, null, ex);
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(NoteRecord note, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(note, nameof(note));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (note.Id != default) {
            var existing = await context.Notes.FindAsync([note.Id], ct).ConfigureAwait(false);
            if (existing != null) {
                existing.ForEntityType = note.ForEntityType;
                existing.ForEntityId = note.ForEntityId;
                existing.FromEntityType = note.FromEntityType;
                existing.FromEntityId = note.FromEntityId;
                existing.Content = note.Content;
                existing.UpdatedTimestamp = DateTime.UtcNow;
                await context.SaveChangesAsync(ct).ConfigureAwait(false);
                return;
            }
        }

        var entity = new NoteEntity {
            Id = note.Id == default ? Guid.NewGuid() : note.Id,
            ForEntityType = note.ForEntityType,
            ForEntityId = note.ForEntityId,
            FromEntityType = note.FromEntityType,
            FromEntityId = note.FromEntityId,
            Content = note.Content,
            CreatedTimestamp = note.CreatedTimestamp == default ? DateTime.UtcNow : note.CreatedTimestamp
        };

        context.Notes.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<NoteRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Notes.FindAsync([id], ct).ConfigureAwait(false);
        return entity == null ? null : ToRecord(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NoteRecord>> GetForEntityAsync(EntityRef forEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity, nameof(forEntity));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Notes.Where(n => n.ForEntityType == forEntity.EntityType && n.ForEntityId == forEntity.EntityId)
            .OrderBy(n => n.CreatedTimestamp)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NoteRecord>> GetFromEntityAsync(EntityRef fromEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(fromEntity, nameof(fromEntity));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Notes.Where(n => n.FromEntityType == fromEntity.EntityType && n.FromEntityId == fromEntity.EntityId)
            .OrderBy(n => n.CreatedTimestamp)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NoteRecord>> GetForEntityTypeAsync(string forEntityType, string? forEntityId = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(forEntityType, nameof(forEntityType));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Notes.Where(n => n.ForEntityType == forEntityType);
        if (!string.IsNullOrWhiteSpace(forEntityId))
            query = query.Where(n => n.ForEntityId == forEntityId);

        var entities = await query.OrderBy(n => n.CreatedTimestamp).ToListAsync(ct).ConfigureAwait(false);
        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Notes.FindAsync([id], ct).ConfigureAwait(false);
        if (entity != null) {
            context.Notes.Remove(entity);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task DeleteForEntityAsync(EntityRef forEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity, nameof(forEntity));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Notes.Where(n => n.ForEntityType == forEntity.EntityType && n.ForEntityId == forEntity.EntityId).ToListAsync(ct).ConfigureAwait(false);
        context.Notes.RemoveRange(entities);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static NoteRecord ToRecord(NoteEntity e)
        => new() {
            Id = e.Id,
            ForEntityType = e.ForEntityType,
            ForEntityId = e.ForEntityId,
            FromEntityType = e.FromEntityType,
            FromEntityId = e.FromEntityId,
            Content = e.Content,
            CreatedTimestamp = e.CreatedTimestamp,
            UpdatedTimestamp = e.UpdatedTimestamp
        };
}