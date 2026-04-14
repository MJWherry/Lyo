using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.ApiEndpoint.Dynamic;

/// <summary>Options for dynamic endpoint registration that maps all entities in a DbContext.</summary>
public sealed class DynamicEndpointOptions<TContext>
    where TContext : DbContext
{
    /// <summary>Base route prefix (e.g. "/api"). Default "".</summary>
    public string BaseRoute { get; set; } = "";

    /// <summary>Feature flags for each entity. Default ApiFeatureFlag.All with bulk and upsert inheritance.</summary>
    public ApiFeatureFlag Features { get; init; } =
        ApiFeatureFlag.All | ApiFeatureFlag.UpsertInheritCreate | ApiFeatureFlag.UpsertInheritUpdate | ApiFeatureFlag.PatchInheritsUpdate;

    /// <summary>When non-empty, only these entity types are registered (whitelist). When empty, all entities in the context are registered.</summary>
    public List<Type> IncludedTypes { get; } = [];

    /// <summary>Entity types to exclude from registration (e.g. xref/junction tables).</summary>
    public HashSet<Type> ExcludedTypes { get; init; } = [];

    /// <summary>Custom route overrides: entity type -> (route, groupName).</summary>
    public Dictionary<Type, (string Route, string GroupName)> RouteOverrides { get; init; } = [];

    /// <summary>When true, use entity as both request and response (no DTOs). Default true for dynamic endpoints.</summary>
    public bool UseEntityAsRequestResponse { get; init; } = true;

    /// <summary>Exclude an entity type from registration.</summary>
    public DynamicEndpointOptions<TContext> Exclude<TEntity>()
        where TEntity : class
    {
        ExcludedTypes.Add(typeof(TEntity));
        return this;
    }

    /// <summary>Only register CRUD for these entity types (whitelist). When null/empty, all entities are considered.</summary>
    public DynamicEndpointOptions<TContext> IncludeOnly(params Type[] types)
    {
        IncludedTypes.AddRange(types);
        return this;
    }

    /// <summary>Only register CRUD for this entity type (whitelist). Chain for multiple.</summary>
    public DynamicEndpointOptions<TContext> IncludeOnly<TEntity>()
        where TEntity : class
    {
        IncludedTypes.Add(typeof(TEntity));
        return this;
    }

    /// <summary>Only register CRUD for these entity types (whitelist).</summary>
    public DynamicEndpointOptions<TContext> IncludeOnly<T1, T2>()
        where T1 : class where T2 : class
    {
        IncludedTypes.AddRange([typeof(T1), typeof(T2)]);
        return this;
    }

    /// <summary>Only register CRUD for these entity types (whitelist).</summary>
    public DynamicEndpointOptions<TContext> IncludeOnly<T1, T2, T3>()
        where T1 : class where T2 : class where T3 : class
    {
        IncludedTypes.AddRange([typeof(T1), typeof(T2), typeof(T3)]);
        return this;
    }

    /// <summary>Only register CRUD for these entity types (whitelist).</summary>
    public DynamicEndpointOptions<TContext> IncludeOnly<T1, T2, T3, T4>()
        where T1 : class where T2 : class where T3 : class where T4 : class
    {
        IncludedTypes.AddRange([typeof(T1), typeof(T2), typeof(T3), typeof(T4)]);
        return this;
    }

    /// <summary>Only register CRUD for these entity types (whitelist).</summary>
    public DynamicEndpointOptions<TContext> IncludeOnly<T1, T2, T3, T4, T5>()
        where T1 : class where T2 : class where T3 : class where T4 : class where T5 : class
    {
        IncludedTypes.AddRange([typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5)]);
        return this;
    }

    /// <summary>Override route and group for an entity.</summary>
    public DynamicEndpointOptions<TContext> OverrideRoute<TEntity>(string route, string groupName)
        where TEntity : class
    {
        RouteOverrides[typeof(TEntity)] = (route, groupName);
        return this;
    }
}