using Lyo.Api.ApiEndpoint.Config;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.ApiEndpoint.Dynamic;

/// <summary>Resolved settings for dynamic CRUD endpoints. Built from defaults plus per-entity overrides.</summary>
public sealed class DynamicEndpointConfig<TContext>
    where TContext : DbContext
{
    /// <summary>Default settings applied to every entity unless overridden.</summary>
    public DynamicEndpointDefaults Defaults { get; }

    /// <summary>Per-entity setting overrides keyed by entity type.</summary>
    public IReadOnlyDictionary<Type, EntityEndpointConfig<TContext>> EntityConfigs { get; }

    internal DynamicEndpointConfig(DynamicEndpointDefaults defaults, IReadOnlyDictionary<Type, EntityEndpointConfig<TContext>> entityConfigs)
    {
        Defaults = defaults;
        EntityConfigs = entityConfigs;
    }

    /// <summary>Returns the merged settings for an entity type (defaults plus entity overrides).</summary>
    public EntityEndpointConfig<TContext> GetConfig(Type entityType)
        => EntityConfigs.TryGetValue(entityType, out var entityConfig)
            ? EntityEndpointConfig<TContext>.Merge(Defaults.ToEntityConfig<TContext>(), entityConfig)
            : Defaults.ToEntityConfig<TContext>();
}

/// <summary>Default settings applied to every entity. Configure via DynamicEndpointConfigBuilder.WithDefaults.</summary>
public sealed class DynamicEndpointDefaults
{
    public ApiFeatureSet Features { get; set; } = ApiFeatureSet.DefaultCrud;

    public Action<CreateContext<object, object, DbContext>>? BeforeCreate { get; set; } // Used when no per-entity override; DbContext base for defaults

    /// <summary>Base route prefix (for example "/api"). Default "".</summary>
    public string BaseRoute { get; set; } = "";

    /// <summary>Entity types left out of registration.</summary>
    public HashSet<Type> ExcludedTypes { get; } = [];

    /// <summary>When non-empty, only these entity types are registered. When empty, every entity.</summary>
    public List<Type> IncludedTypes { get; } = [];

    /// <summary>
    /// Field names rejected wherever a dynamic query can reach them: select, filter, sort, include, and join paths. Dynamic routes read raw entities, so a column that response
    /// mapping would have masked is otherwise returned as-is.
    /// </summary>
    public List<string> DeniedSelectFields { get; } = [];

    /// <summary>
    /// Authorization applied to every dynamic route that does not override it. Required: mapping throws when this is null, because the dynamic builder exposes delete, upsert, and
    /// property-reflecting metadata routes, and silently defaulting those to anonymous is not a safe guess. Set <see cref="EndpointAuth.Anonymous" /> to opt in on purpose.
    /// </summary>
    public EndpointAuth? Auth { get; set; }

    /// <summary>
    /// Request-shape limits applied to the dynamic query routes, the same way <c>QueryConfig</c> limits the typed ones. Defaults are deliberately generous but finite. The dynamic
    /// routes are reachable for every registered entity, so an unbounded include or select list there is a cheap way to make the database do unbounded work.
    /// </summary>
    public QueryPolicy QueryPolicy { get; set; } = new() {
        MaxIncludePathCount = 20,
        MaxIncludePageSize = 200,
        MaxKeySetCount = 100,
        MaxSelectFieldCount = 100,
        MaxComputedFieldCount = 25,
        MaxComputedTemplateLength = 2048,
    };

    internal EntityEndpointConfig<TContext> ToEntityConfig<TContext>()
        where TContext : DbContext
        => new(Features, null, null, null, null, null, null);
}

/// <summary>Resolved settings for one entity. Merged from defaults plus entity overrides. Uses object for the entity type.</summary>
public sealed class EntityEndpointConfig<TContext>
    where TContext : DbContext
{
    public ApiFeatureSet Features { get; }

    public CreateConfig<object, object, TContext>? CreateConfig { get; }

    public PatchConfig<object, TContext>? PatchConfig { get; }

    public UpdateConfig<object, object, TContext>? UpdateConfig { get; }

    public DeleteConfig<object, TContext>? DeleteConfig { get; }

    public UpsertConfig<object, object, TContext>? UpsertConfig { get; }

    public ExportConfig<object>? ExportConfig { get; }

    internal EntityEndpointConfig(
        ApiFeatureSet features,
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