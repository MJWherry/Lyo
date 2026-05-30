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
        IOptions<PostgresNoteOptions> noteOptions,
        IEnumerable<IEntityRefActionInterceptor>? interceptors = null)
        : base(entityRefOptions, noteOptions?.Value.Tenancy ?? throw new ArgumentNullException(nameof(noteOptions)), interceptors)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory);
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
    public async Task SaveAsync(NoteRecord note, Guid? tenantId = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(note);
        var tenant = ResolveTenant(tenantId);
        var forId = EntityRefPersistedGuid.PersistedEntityId(note.SubjectRef);
        var fromId = EntityRefPersistedGuid.PersistedEntityId(note.ActorRef);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (note.Id != default) {
            var existing = await context.Notes.WhereActive().WhereTenant(tenant).FirstOrDefaultAsync(n => n.Id == note.Id, ct).ConfigureAwait(false);
            if (existing != null) {
                existing.SubjectEntityType = note.SubjectEntityType;
                existing.SubjectEntityId = forId;
                existing.ActorEntityType = note.ActorEntityType;
                existing.ActorEntityId = fromId;
                existing.Content = note.Content;
                await RunInterceptorsAsync(ModuleKey, tenant, EntityRefActionKind.BeforePersist, existing, ct).ConfigureAwait(false);
                await context.SaveChangesAsync(ct).ConfigureAwait(false);
                await RunInterceptorsAsync(ModuleKey, tenant, EntityRefActionKind.AfterPersist, existing, ct).ConfigureAwait(false);
                return;
            }
        }

        var entity = new NoteEntity {
            Id = note.Id == default ? Guid.NewGuid() : note.Id,
            SubjectEntityType = note.SubjectEntityType,
            SubjectEntityId = forId,
            ActorEntityType = note.ActorEntityType,
            ActorEntityId = fromId,
            TenantId = tenant,
            Content = note.Content,
            Visibility = string.IsNullOrWhiteSpace(note.Visibility) ? EntityRefVisibility.Private : note.Visibility,
            CreatedAt = note.CreatedAt == default ? DateTime.UtcNow : note.CreatedAt
        };

        await RunInterceptorsAsync(ModuleKey, tenant, EntityRefActionKind.BeforePersist, entity, ct).ConfigureAwait(false);
        context.Notes.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        await RunInterceptorsAsync(ModuleKey, tenant, EntityRefActionKind.AfterPersist, entity, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<NoteRecord?> GetByIdAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default)
    {
        var tenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Notes.WhereActive().WhereTenant(tenant).FirstOrDefaultAsync(n => n.Id == id, ct).ConfigureAwait(false);
        return entity == null ? null : ToRecord(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NoteRecord>> GetForEntityAsync(EntityRef forEntity, Guid? tenantId = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity);
        var tenant = ResolveTenant(tenantId);
        var forId = EntityRefPersistedGuid.PersistedEntityId(forEntity);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Notes.WhereActive()
            .WhereTenant(tenant)
            .Where(n => n.SubjectEntityType == forEntity.EntityType && n.SubjectEntityId == forId)
            .OrderBy(n => n.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NoteRecord>> GetFromEntityAsync(EntityRef fromEntity, Guid? tenantId = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(fromEntity);
        var tenant = ResolveTenant(tenantId);
        var fromId = EntityRefPersistedGuid.PersistedEntityId(fromEntity);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Notes.WhereActive()
            .WhereTenant(tenant)
            .Where(n => n.ActorEntityType == fromEntity.EntityType && n.ActorEntityId == fromId)
            .OrderBy(n => n.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NoteRecord>> GetForEntityTypeAsync(string forEntityType, Guid? forEntityId = null, Guid? tenantId = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(forEntityType);
        var tenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Notes.WhereActive().WhereTenant(tenant).Where(n => n.SubjectEntityType == forEntityType);
        if (forEntityId.HasValue)
            query = query.Where(n => n.SubjectEntityId == forEntityId.Value.ToString());

        var entities = await query.OrderBy(n => n.CreatedAt).ToListAsync(ct).ConfigureAwait(false);
        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default)
    {
        var tenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Notes.WhereActive().WhereTenant(tenant).FirstOrDefaultAsync(n => n.Id == id, ct).ConfigureAwait(false);
        if (entity != null) {
            await RunInterceptorsAsync(ModuleKey, tenant, EntityRefActionKind.BeforeSoftDelete, entity, ct).ConfigureAwait(false);
            entity.DeletedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
            await RunInterceptorsAsync(ModuleKey, tenant, EntityRefActionKind.AfterSoftDelete, entity, ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task DeleteForEntityAsync(EntityRef forEntity, Guid? tenantId = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity);
        var tenant = ResolveTenant(tenantId);
        var forId = EntityRefPersistedGuid.PersistedEntityId(forEntity);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Notes.WhereActive()
            .WhereTenant(tenant)
            .Where(n => n.SubjectEntityType == forEntity.EntityType && n.SubjectEntityId == forId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var utc = DateTime.UtcNow;
        foreach (var e in entities)
            await RunInterceptorsAsync(ModuleKey, tenant, EntityRefActionKind.BeforeSoftDelete, e, ct).ConfigureAwait(false);

        foreach (var e in entities)
            e.DeletedAt = utc;

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        foreach (var e in entities)
            await RunInterceptorsAsync(ModuleKey, tenant, EntityRefActionKind.AfterSoftDelete, e, ct).ConfigureAwait(false);
    }

    private static NoteRecord ToRecord(NoteEntity e)
        => new() {
            Id = e.Id,
            SubjectEntityType = e.SubjectEntityType,
            SubjectEntityId = e.SubjectEntityId,
            ActorEntityType = e.ActorEntityType,
            ActorEntityId = e.ActorEntityId,
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