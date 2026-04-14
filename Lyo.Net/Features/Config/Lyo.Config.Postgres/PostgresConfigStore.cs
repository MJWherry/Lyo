using System.Diagnostics;
using Lyo.Common;
using Lyo.Config.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Health;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Config.Postgres;

/// <summary>PostgreSQL implementation of IConfigStore.</summary>
public sealed class PostgresConfigStore : IConfigStore, IHealth
{
    private readonly IDbContextFactory<ConfigDbContext> _contextFactory;

    /// <summary>Creates a new PostgresConfigStore.</summary>
    public PostgresConfigStore(IDbContextFactory<ConfigDbContext> contextFactory)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory, nameof(contextFactory));
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public async Task SaveDefinitionAsync(ConfigDefinitionRecord definition, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(definition, nameof(definition));
        definition.Validate();
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        ConfigDefinitionEntity? entity = null;
        if (definition.Id != default)
            entity = await context.ConfigDefinitions.FindAsync([definition.Id], ct).ConfigureAwait(false);

        entity ??= await context.ConfigDefinitions.FirstOrDefaultAsync(x => x.ForEntityType == definition.ForEntityType && x.Key == definition.Key, ct).ConfigureAwait(false);
        if (entity != null) {
            entity.ForEntityType = definition.ForEntityType;
            entity.Key = definition.Key;
            entity.ForValueType = definition.ForValueType;
            entity.Description = definition.Description;
            entity.IsRequired = definition.IsRequired;
            entity.DefaultValueJson = definition.DefaultValue?.Json;
            var bindings = await context.ConfigBindings.Where(x => x.DefinitionId == entity.Id).ToListAsync(ct).ConfigureAwait(false);
            foreach (var b in bindings)
                b.ValueType = entity.ForValueType;

            await context.SaveChangesAsync(ct).ConfigureAwait(false);
            definition.Id = entity.Id;
            definition.CreatedTimestamp = entity.CreatedTimestamp;
            definition.UpdatedTimestamp = entity.UpdatedTimestamp;
            return;
        }

        entity = new() {
            Id = definition.Id == default ? Guid.NewGuid() : definition.Id,
            ForEntityType = definition.ForEntityType,
            Key = definition.Key,
            ForValueType = definition.ForValueType,
            Description = definition.Description,
            IsRequired = definition.IsRequired,
            DefaultValueJson = definition.DefaultValue?.Json,
            CreatedTimestamp = definition.CreatedTimestamp == default ? DateTime.UtcNow : definition.CreatedTimestamp
        };

        context.ConfigDefinitions.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        definition.Id = entity.Id;
        definition.CreatedTimestamp = entity.CreatedTimestamp;
        definition.UpdatedTimestamp = entity.UpdatedTimestamp;
    }

    /// <inheritdoc />
    public async Task<ConfigDefinitionRecord?> GetDefinitionByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.ConfigDefinitions.FindAsync([id], ct).ConfigureAwait(false);
        return entity == null ? null : ToRecord(entity);
    }

    /// <inheritdoc />
    public async Task<ConfigDefinitionRecord?> GetDefinitionAsync(string forEntityType, string key, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(forEntityType, nameof(forEntityType));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key, nameof(key));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.ConfigDefinitions.FirstOrDefaultAsync(x => x.ForEntityType == forEntityType && x.Key == key, ct).ConfigureAwait(false);
        return entity == null ? null : ToRecord(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConfigDefinitionRecord>> GetDefinitionsAsync(string forEntityType, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(forEntityType, nameof(forEntityType));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.ConfigDefinitions.Where(x => x.ForEntityType == forEntityType).OrderBy(x => x.Key).ToListAsync(ct).ConfigureAwait(false);
        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    /// <remarks>Removes the definition row; dependent <c>config_binding</c> rows are removed by foreign-key cascade.</remarks>
    public async Task DeleteDefinitionAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.ConfigDefinitions.FindAsync([id], ct).ConfigureAwait(false);
        if (entity != null) {
            context.ConfigDefinitions.Remove(entity);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task SaveBindingAsync(ConfigBindingRecord binding, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(binding, nameof(binding));
        ArgumentHelpers.ThrowIfNull(binding.Value, nameof(binding.Value));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(binding.Key, nameof(binding.Key));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(binding.ForEntityType, nameof(binding.ForEntityType));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(binding.ForEntityId, nameof(binding.ForEntityId));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var definition = binding.DefinitionId != default
            ? await context.ConfigDefinitions.FindAsync([binding.DefinitionId], ct).ConfigureAwait(false)
            : await context.ConfigDefinitions.FirstOrDefaultAsync(x => x.ForEntityType == binding.ForEntityType && x.Key == binding.Key, ct).ConfigureAwait(false);

        OperationHelpers.ThrowIfNull(definition, $"No config definition exists for entity type '{binding.ForEntityType}' and key '{binding.Key}'.");
        OperationHelpers.ThrowIf(
            !string.Equals(definition.ForEntityType, binding.ForEntityType, StringComparison.Ordinal),
            $"Binding entity type '{binding.ForEntityType}' does not match definition entity type '{definition.ForEntityType}'.");

        OperationHelpers.ThrowIf(
            !binding.Value.MatchesType(definition.ForValueType), $"Binding value type '{binding.Value.TypeName}' does not match definition type '{definition.ForValueType}'.");

        ConfigBindingEntity? entity = null;
        if (binding.Id != default)
            entity = await context.ConfigBindings.FindAsync([binding.Id], ct).ConfigureAwait(false);

        entity ??= await context.ConfigBindings.FirstOrDefaultAsync(
                x => x.DefinitionId == definition.Id && x.ForEntityType == binding.ForEntityType && x.ForEntityId == binding.ForEntityId, ct)
            .ConfigureAwait(false);

        if (entity != null) {
            entity.Key = definition.Key;
            entity.DefinitionId = definition.Id;
            entity.ForEntityType = binding.ForEntityType;
            entity.ForEntityId = binding.ForEntityId;
            entity.ValueType = definition.ForValueType;
        }
        else {
            entity = new() {
                Id = binding.Id == default ? Guid.NewGuid() : binding.Id,
                DefinitionId = definition.Id,
                Key = definition.Key,
                ForEntityType = binding.ForEntityType,
                ForEntityId = binding.ForEntityId,
                ValueType = definition.ForValueType,
                CreatedTimestamp = binding.CreatedTimestamp == default ? DateTime.UtcNow : binding.CreatedTimestamp
            };

            context.ConfigBindings.Add(entity);
        }

        await AppendRevisionRowAsync(context, entity, binding.Value.Json, ct).ConfigureAwait(false);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        binding.Id = entity.Id;
        binding.DefinitionId = entity.DefinitionId;
        binding.Key = entity.Key;
        binding.CreatedTimestamp = entity.CreatedTimestamp;
        binding.UpdatedTimestamp = entity.UpdatedTimestamp;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConfigBindingRevisionRecord>> GetBindingRevisionsAsync(Guid bindingId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var bindingEntity = await context.ConfigBindings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == bindingId, ct).ConfigureAwait(false);
        OperationHelpers.ThrowIfNull(bindingEntity);
        var definition = await context.ConfigDefinitions.AsNoTracking().FirstAsync(x => x.Id == bindingEntity.DefinitionId, ct).ConfigureAwait(false);
        var rows = await context.ConfigBindingRevisions.AsNoTracking()
            .Where(x => x.BindingId == bindingId)
            .OrderByDescending(x => x.Revision)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows.Select(r => ToRevisionRecord(r, ResolveBindingValueType(bindingEntity, definition.ForValueType))).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConfigBindingRevisionRecord>> GetBindingRevisionsAsync(EntityRef forEntity, string key, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity, nameof(forEntity));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key, nameof(key));
        var binding = await GetBindingAsync(forEntity, key, ct).ConfigureAwait(false);
        OperationHelpers.ThrowIfNull(binding, $"No binding for entity type '{forEntity.EntityType}' id '{forEntity.EntityId}' and key '{key}'.");
        return await GetBindingRevisionsAsync(binding.Id, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ConfigBindingRevisionRecord?> GetBindingRevisionAsync(Guid bindingId, int revision, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var bindingEntity = await context.ConfigBindings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == bindingId, ct).ConfigureAwait(false);
        if (bindingEntity == null)
            return null;

        var definition = await context.ConfigDefinitions.AsNoTracking().FirstAsync(x => x.Id == bindingEntity.DefinitionId, ct).ConfigureAwait(false);
        var row = await context.ConfigBindingRevisions.AsNoTracking().FirstOrDefaultAsync(x => x.BindingId == bindingId && x.Revision == revision, ct).ConfigureAwait(false);
        return row == null ? null : ToRevisionRecord(row, ResolveBindingValueType(bindingEntity, definition.ForValueType));
    }

    /// <inheritdoc />
    public async Task RevertBindingToRevisionAsync(Guid bindingId, int revision, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var binding = await context.ConfigBindings.FindAsync([bindingId], ct).ConfigureAwait(false);
        OperationHelpers.ThrowIfNull(binding);
        var revEntity = await context.ConfigBindingRevisions.FirstOrDefaultAsync(x => x.BindingId == bindingId && x.Revision == revision, ct).ConfigureAwait(false);
        OperationHelpers.ThrowIfNull(revEntity, $"No revision {revision} for binding '{bindingId}'.");
        var definition = await context.ConfigDefinitions.FindAsync([binding.DefinitionId], ct).ConfigureAwait(false);
        OperationHelpers.ThrowIfNull(definition);
        var valueType = ResolveBindingValueType(binding, definition.ForValueType);
        var cv = new ConfigValue { TypeName = valueType, Json = revEntity.ValueJson };
        OperationHelpers.ThrowIf(!cv.MatchesType(definition.ForValueType), "Revision JSON does not match the current definition type.");
        await AppendRevisionRowAsync(context, binding, revEntity.ValueJson, ct).ConfigureAwait(false);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RevertBindingToRevisionAsync(EntityRef forEntity, string key, int revision, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity, nameof(forEntity));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key, nameof(key));
        var binding = await GetBindingAsync(forEntity, key, ct).ConfigureAwait(false);
        OperationHelpers.ThrowIfNull(binding, $"No binding for entity type '{forEntity.EntityType}' id '{forEntity.EntityId}' and key '{key}'.");
        await RevertBindingToRevisionAsync(binding.Id, revision, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ConfigBindingRecord?> GetBindingByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.ConfigBindings.FindAsync([id], ct).ConfigureAwait(false);
        return entity == null ? null : await ToBindingRecordAsync(context, entity, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ConfigBindingRecord?> GetBindingAsync(EntityRef forEntity, string key, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity, nameof(forEntity));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key, nameof(key));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.ConfigBindings.FirstOrDefaultAsync(x => x.ForEntityType == forEntity.EntityType && x.ForEntityId == forEntity.EntityId && x.Key == key, ct)
            .ConfigureAwait(false);

        return entity == null ? null : await ToBindingRecordAsync(context, entity, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConfigBindingRecord>> GetBindingsAsync(EntityRef forEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity, nameof(forEntity));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.ConfigBindings.Where(x => x.ForEntityType == forEntity.EntityType && x.ForEntityId == forEntity.EntityId)
            .OrderBy(x => x.Key)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (entities.Count == 0)
            return [];

        var latestByBinding = await LoadLatestRevisionJsonByBindingIdAsync(context, entities.Select(e => e.Id).ToList(), ct).ConfigureAwait(false);
        var defIds = entities.Select(e => e.DefinitionId).Distinct().ToList();
        var defsById = await context.ConfigDefinitions.AsNoTracking().Where(d => defIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, ct).ConfigureAwait(false);
        var result = new List<ConfigBindingRecord>(entities.Count);
        foreach (var entity in entities)
            result.Add(ToBindingRecord(entity, ResolveBindingValueType(entity, defsById[entity.DefinitionId].ForValueType), latestByBinding[entity.Id]));

        return result;
    }

    /// <inheritdoc />
    public async Task DeleteBindingAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.ConfigBindings.FindAsync([id], ct).ConfigureAwait(false);
        if (entity == null)
            return;

        var definition = await context.ConfigDefinitions.FindAsync([entity.DefinitionId], ct).ConfigureAwait(false);
        OperationHelpers.ThrowIfNull(definition);
        EnsureBindingMayBeDeleted(definition);
        context.ConfigBindings.Remove(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteBindingsAsync(EntityRef forEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity, nameof(forEntity));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.ConfigBindings.Where(x => x.ForEntityType == forEntity.EntityType && x.ForEntityId == forEntity.EntityId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var entity in entities) {
            var definition = await context.ConfigDefinitions.FindAsync([entity.DefinitionId], ct).ConfigureAwait(false);
            OperationHelpers.ThrowIfNull(definition);
            EnsureBindingMayBeDeleted(definition);
        }

        context.ConfigBindings.RemoveRange(entities);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ResolvedConfigRecord> LoadConfigAsync(EntityRef forEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity, nameof(forEntity));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var definitions = await context.ConfigDefinitions.Where(x => x.ForEntityType == forEntity.EntityType).OrderBy(x => x.Key).ToListAsync(ct).ConfigureAwait(false);
        var bindings = await context.ConfigBindings.Where(x => x.ForEntityType == forEntity.EntityType && x.ForEntityId == forEntity.EntityId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var bindingsByDefinitionId = bindings.ToDictionary(x => x.DefinitionId);
        IReadOnlyDictionary<Guid, string>? latestJson = null;
        if (bindings.Count > 0)
            latestJson = await LoadLatestRevisionJsonByBindingIdAsync(context, bindings.Select(b => b.Id).ToList(), ct).ConfigureAwait(false);

        var items = new List<ResolvedConfigItemRecord>(definitions.Count);
        foreach (var definition in definitions) {
            ConfigBindingRecord? bindingRecord = null;
            if (bindingsByDefinitionId.TryGetValue(definition.Id, out var bindingEntity) && latestJson != null)
                bindingRecord = ToBindingRecord(bindingEntity, ResolveBindingValueType(bindingEntity, definition.ForValueType), latestJson[bindingEntity.Id]);

            items.Add(new() { Definition = ToRecord(definition), Binding = bindingRecord });
        }

        var resolved = new ResolvedConfigRecord { ForEntityType = forEntity.EntityType, ForEntityId = forEntity.EntityId, Items = items };
        resolved.ValidateRequired();
        return resolved;
    }

    /// <inheritdoc />
    public string HealthCheckName => "config-postgres";

    /// <inheritdoc />
    public async Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try {
            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var canConnect = await context.Database.CanConnectAsync(ct).ConfigureAwait(false);
            sw.Stop();
            return canConnect
                ? HealthResult.Healthy(sw.Elapsed, null, new Dictionary<string, object?> { ["database"] = "config" })
                : HealthResult.Unhealthy(sw.Elapsed, "Database connection failed");
        }
        catch (Exception ex) {
            sw.Stop();
            return HealthResult.Unhealthy(sw.Elapsed, ex.Message, null, ex);
        }
    }

    private static async Task AppendRevisionRowAsync(ConfigDbContext context, ConfigBindingEntity entity, string valueJson, CancellationToken ct)
    {
        var maxRev = await context.ConfigBindingRevisions.Where(x => x.BindingId == entity.Id).Select(x => (int?)x.Revision).MaxAsync(ct).ConfigureAwait(false) ?? 0;
        var next = maxRev + 1;
        context.ConfigBindingRevisions.Add(
            new() {
                BindingId = entity.Id,
                Revision = next,
                ValueJson = valueJson,
                CreatedTimestamp = DateTime.UtcNow
            });
    }

    private static async Task<Dictionary<Guid, string>> LoadLatestRevisionJsonByBindingIdAsync(ConfigDbContext context, IReadOnlyList<Guid> bindingIds, CancellationToken ct)
    {
        if (bindingIds.Count == 0)
            return new();

        var rows = await context.ConfigBindingRevisions.AsNoTracking().Where(r => bindingIds.Contains(r.BindingId)).ToListAsync(ct).ConfigureAwait(false);
        var map = new Dictionary<Guid, string>(bindingIds.Count);
        foreach (var id in bindingIds) {
            var best = rows.Where(r => r.BindingId == id).OrderByDescending(r => r.Revision).FirstOrDefault();
            OperationHelpers.ThrowIfNull(best, $"Binding '{id}' has no revision rows.");
            map[id] = best.ValueJson;
        }

        return map;
    }

    private static async Task<ConfigBindingRecord> ToBindingRecordAsync(ConfigDbContext context, ConfigBindingEntity entity, CancellationToken ct)
    {
        var definition = await context.ConfigDefinitions.AsNoTracking().FirstAsync(d => d.Id == entity.DefinitionId, ct).ConfigureAwait(false);
        var latest = await context.ConfigBindingRevisions.AsNoTracking()
            .Where(r => r.BindingId == entity.Id)
            .OrderByDescending(r => r.Revision)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        OperationHelpers.ThrowIfNull(latest, $"Binding '{entity.Id}' has no revision rows.");
        return ToBindingRecord(entity, ResolveBindingValueType(entity, definition.ForValueType), latest.ValueJson);
    }

    /// <summary>Uses <see cref="ConfigBindingEntity.ValueType" /> when set; otherwise the definition (for legacy rows missing the column).</summary>
    private static string ResolveBindingValueType(ConfigBindingEntity entity, string definitionForValueType)
        => string.IsNullOrWhiteSpace(entity.ValueType) ? definitionForValueType : entity.ValueType;

    private static ConfigBindingRecord ToBindingRecord(ConfigBindingEntity entity, string valueType, string valueJson)
        => new() {
            Id = entity.Id,
            DefinitionId = entity.DefinitionId,
            Key = entity.Key,
            ForEntityType = entity.ForEntityType,
            ForEntityId = entity.ForEntityId,
            Value = new() { TypeName = valueType, Json = valueJson },
            CreatedTimestamp = entity.CreatedTimestamp,
            UpdatedTimestamp = entity.UpdatedTimestamp
        };

    private static void EnsureBindingMayBeDeleted(ConfigDefinitionEntity definition)
        => OperationHelpers.ThrowIf(
            definition.IsRequired && definition.DefaultValueJson == null,
            $"Cannot delete binding for required config key '{definition.Key}' (no default is defined). Add a default, set IsRequired to false, or delete the definition instead.");

    private static ConfigBindingRevisionRecord ToRevisionRecord(ConfigBindingRevisionEntity entity, string valueType)
        => new() {
            BindingId = entity.BindingId,
            Revision = entity.Revision,
            Value = new() { TypeName = valueType, Json = entity.ValueJson },
            CreatedTimestamp = entity.CreatedTimestamp
        };

    private static ConfigDefinitionRecord ToRecord(ConfigDefinitionEntity entity)
        => new() {
            Id = entity.Id,
            ForEntityType = entity.ForEntityType,
            Key = entity.Key,
            ForValueType = entity.ForValueType,
            Description = entity.Description,
            IsRequired = entity.IsRequired,
            DefaultValue = entity.DefaultValueJson == null ? null : new ConfigValue { TypeName = entity.ForValueType, Json = entity.DefaultValueJson },
            CreatedTimestamp = entity.CreatedTimestamp,
            UpdatedTimestamp = entity.UpdatedTimestamp
        };
}