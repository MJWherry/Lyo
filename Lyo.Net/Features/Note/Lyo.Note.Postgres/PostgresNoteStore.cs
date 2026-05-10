using System.Diagnostics;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.Exceptions;
using Lyo.Health;
using Lyo.Note.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lyo.Note.Postgres;

/// <summary>PostgreSQL implementation of INoteStore.</summary>
public sealed class PostgresNoteStore : EntityRefPostgresStoreBase, INoteStore, IHealth
{
    private const string ModuleKey = "Note";

    private readonly IDbContextFactory<NoteDbContext> _contextFactory;

    public PostgresNoteStore(
        IDbContextFactory<NoteDbContext> contextFactory,
        IOptions<EntityRefOptions> entityRefOptions,
        IEnumerable<IEntityRefActionInterceptor>? interceptors = null)
        : base(entityRefOptions, interceptors)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory);
        _contextFactory = contextFactory;
    }

    private Guid Tenant => ResolveTenant(null);

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
        ArgumentHelpers.ThrowIfNull(note);
        var forId = EntityRefPersistedGuid.RequirePersistedGuid(note.ForEntity);
        var fromId = EntityRefPersistedGuid.RequirePersistedGuid(note.FromEntity);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (note.Id != default) {
            var existing = await context.Notes.WhereActive().WhereTenant(Tenant).FirstOrDefaultAsync(n => n.Id == note.Id, ct).ConfigureAwait(false);
            if (existing != null) {
                existing.ForEntityType = note.ForEntityType;
                existing.ForEntityId = forId;
                existing.FromEntityType = note.FromEntityType;
                existing.FromEntityId = fromId;
                existing.Content = note.Content;
                await RunInterceptorsAsync(ModuleKey, Tenant, EntityRefActionKind.BeforePersist, existing, ct).ConfigureAwait(false);
                await context.SaveChangesAsync(ct).ConfigureAwait(false);
                await RunInterceptorsAsync(ModuleKey, Tenant, EntityRefActionKind.AfterPersist, existing, ct).ConfigureAwait(false);
                return;
            }
        }

        var entity = new NoteEntity {
            Id = note.Id == default ? Guid.NewGuid() : note.Id,
            ForEntityType = note.ForEntityType,
            ForEntityId = forId,
            FromEntityType = note.FromEntityType,
            FromEntityId = fromId,
            TenantId = Tenant,
            Content = note.Content,
            Visibility = string.IsNullOrWhiteSpace(note.Visibility) ? EntityRefVisibility.Private : note.Visibility,
            CreatedAt = note.CreatedAt == default ? DateTime.UtcNow : note.CreatedAt
        };

        await RunInterceptorsAsync(ModuleKey, Tenant, EntityRefActionKind.BeforePersist, entity, ct).ConfigureAwait(false);
        context.Notes.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        await RunInterceptorsAsync(ModuleKey, Tenant, EntityRefActionKind.AfterPersist, entity, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<NoteRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Notes.WhereActive().WhereTenant(Tenant).FirstOrDefaultAsync(n => n.Id == id, ct).ConfigureAwait(false);
        return entity == null ? null : ToRecord(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NoteRecord>> GetForEntityAsync(EntityRef forEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity);
        var forId = EntityRefPersistedGuid.RequirePersistedGuid(forEntity);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Notes.WhereActive().WhereTenant(Tenant).Where(n => n.ForEntityType == forEntity.EntityType && n.ForEntityId == forId).OrderBy(n => n.CreatedAt).ToListAsync(ct).ConfigureAwait(false);

        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NoteRecord>> GetFromEntityAsync(EntityRef fromEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(fromEntity);
        var fromId = EntityRefPersistedGuid.RequirePersistedGuid(fromEntity);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Notes.WhereActive().WhereTenant(Tenant).Where(n => n.FromEntityType == fromEntity.EntityType && n.FromEntityId == fromId).OrderBy(n => n.CreatedAt).ToListAsync(ct).ConfigureAwait(false);

        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NoteRecord>> GetForEntityTypeAsync(string forEntityType, Guid? forEntityId = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(forEntityType);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Notes.WhereActive().WhereTenant(Tenant).Where(n => n.ForEntityType == forEntityType);
        if (forEntityId.HasValue)
            query = query.Where(n => n.ForEntityId == forEntityId.Value);

        var entities = await query.OrderBy(n => n.CreatedAt).ToListAsync(ct).ConfigureAwait(false);
        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Notes.WhereActive().WhereTenant(Tenant).FirstOrDefaultAsync(n => n.Id == id, ct).ConfigureAwait(false);
        if (entity != null) {
            await RunInterceptorsAsync(ModuleKey, Tenant, EntityRefActionKind.BeforeSoftDelete, entity, ct).ConfigureAwait(false);
            entity.DeletedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
            await RunInterceptorsAsync(ModuleKey, Tenant, EntityRefActionKind.AfterSoftDelete, entity, ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task DeleteForEntityAsync(EntityRef forEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity);
        var forId = EntityRefPersistedGuid.RequirePersistedGuid(forEntity);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Notes.WhereActive().WhereTenant(Tenant).Where(n => n.ForEntityType == forEntity.EntityType && n.ForEntityId == forId).ToListAsync(ct).ConfigureAwait(false);
        var utc = DateTime.UtcNow;
        foreach (var e in entities)
            await RunInterceptorsAsync(ModuleKey, Tenant, EntityRefActionKind.BeforeSoftDelete, e, ct).ConfigureAwait(false);

        foreach (var e in entities)
            e.DeletedAt = utc;

        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        foreach (var e in entities)
            await RunInterceptorsAsync(ModuleKey, Tenant, EntityRefActionKind.AfterSoftDelete, e, ct).ConfigureAwait(false);
    }

    private static NoteRecord ToRecord(NoteEntity e)
        => new() {
            Id = e.Id,
            ForEntityType = e.ForEntityType,
            ForEntityId = e.ForEntityId,
            FromEntityType = e.FromEntityType,
            FromEntityId = e.FromEntityId,
            TenantId = e.TenantId,
            Context = e.Context,
            CreatedAt = e.CreatedAt,
            ExpiresAt = e.ExpiresAt,
            DeletedAt = e.DeletedAt,
            DeletedByType = e.DeletedByType,
            DeletedById = e.DeletedById,
            MetadataJson = e.MetadataJson,
            Visibility = e.Visibility,
            Content = e.Content,
            UpdatedTimestamp = e.UpdatedTimestamp
        };
}
