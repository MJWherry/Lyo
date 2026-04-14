using Lyo.Api.ApiEndpoint.Config;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.ApiEndpoint.Dynamic;

/// <summary>Resolved configuration for dynamic CRUD endpoints. Built from defaults + per-entity overrides.</summary>
public sealed class DynamicEndpointConfig<TContext>
    where TContext : DbContext
{
    /// <summary>Default settings applied to all entities unless overridden.</summary>
    public DynamicEndpointDefaults Defaults { get; }

    /// <summary>Per-entity config overrides keyed by entity type.</summary>
    public IReadOnlyDictionary<Type, EntityEndpointConfig<TContext>> EntityConfigs { get; }

    internal DynamicEndpointConfig(DynamicEndpointDefaults defaults, IReadOnlyDictionary<Type, EntityEndpointConfig<TContext>> entityConfigs)
    {
        Defaults = defaults;
        EntityConfigs = entityConfigs;
    }

    /// <summary>Gets the merged config for an entity type (defaults + entity overrides).</summary>
    public EntityEndpointConfig<TContext> GetConfig(Type entityType)
        => EntityConfigs.TryGetValue(entityType, out var entityConfig)
            ? EntityEndpointConfig<TContext>.Merge(Defaults.ToEntityConfig<TContext>(), entityConfig)
            : Defaults.ToEntityConfig<TContext>();
}

/// <summary>Default settings applied to all entities. Configure via DynamicEndpointConfigBuilder.WithDefaults.</summary>
public sealed class DynamicEndpointDefaults
{
    public ApiFeatureFlag Features { get; set; } =
        ApiFeatureFlag.All | ApiFeatureFlag.UpsertInheritCreate | ApiFeatureFlag.UpsertInheritUpdate | ApiFeatureFlag.PatchInheritsUpdate;

    public Action<CreateContext<object, object, DbContext>>? BeforeCreate { get; set; } // Used when no per-entity override; DbContext base for defaults

    /// <summary>Base route prefix (e.g. "/api"). Default "".</summary>
    public string BaseRoute { get; set; } = "";

    /// <summary>Entity types to exclude from registration.</summary>
    public HashSet<Type> ExcludedTypes { get; } = [];

    /// <summary>When non-empty, only these entity types are registered. When empty, all entities.</summary>
    public List<Type> IncludedTypes { get; } = [];

    internal EntityEndpointConfig<TContext> ToEntityConfig<TContext>()
        where TContext : DbContext
        => new(Features, null, null, null, null, null, null);
}

/// <summary>Resolved configuration for a single entity. Merged from defaults + entity overrides. Uses object for entity type.</summary>
public sealed class EntityEndpointConfig<TContext>
    where TContext : DbContext
{
    public ApiFeatureFlag Features { get; }

    public CreateConfig<object, object, TContext>? CreateConfig { get; }

    public PatchConfig<object, TContext>? PatchConfig { get; }

    public UpdateConfig<object, object, TContext>? UpdateConfig { get; }

    public DeleteConfig<object, TContext>? DeleteConfig { get; }

    public UpsertConfig<object, object, TContext>? UpsertConfig { get; }

    public ExportConfig<object>? ExportConfig { get; }

    internal EntityEndpointConfig(
        ApiFeatureFlag features,
        CreateConfig<object, object, TContext>? createConfig,
        PatchConfig<object, TContext>? patchConfig,
        UpdateConfig<object, object, TContext>? updateConfig,
        DeleteConfig<object, TContext>? deleteConfig,
        UpsertConfig<object, object, TContext>? upsertConfig,
        ExportConfig<object>? exportConfig)
    {
        Features = features;
        CreateConfig = createConfig;
        PatchConfig = patchConfig;
        UpdateConfig = updateConfig;
        DeleteConfig = deleteConfig;
        UpsertConfig = upsertConfig;
        ExportConfig = exportConfig;
    }

    internal static EntityEndpointConfig<TContext> Merge(EntityEndpointConfig<TContext> defaults, EntityEndpointConfig<TContext> overrides)
        => new(
            overrides.Features, overrides.CreateConfig ?? defaults.CreateConfig, overrides.PatchConfig ?? defaults.PatchConfig, overrides.UpdateConfig ?? defaults.UpdateConfig,
            overrides.DeleteConfig ?? defaults.DeleteConfig, overrides.UpsertConfig ?? defaults.UpsertConfig, overrides.ExportConfig ?? defaults.ExportConfig);
}