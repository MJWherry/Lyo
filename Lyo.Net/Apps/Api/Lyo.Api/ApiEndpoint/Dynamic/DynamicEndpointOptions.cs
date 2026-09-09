using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.ApiEndpoint.Dynamic;

/// <summary>Settings for dynamic endpoint registration that maps every entity in a DbContext.</summary>
public sealed class DynamicEndpointOptions<TContext>
    where TContext : DbContext
{
    /// <summary>Base route prefix (for example "/api"). Default "".</summary>
    public string BaseRoute { get; set; } = "";

    /// <summary>Feature flags for each entity. Defaults to <see cref="ApiFeatureSet.DefaultCrud" />.</summary>
    public ApiFeatureSet Features { get; init; } = ApiFeatureSet.DefaultCrud;

    /// <summary>When non-empty, only these entity types are registered (whitelist). When empty, every entity in the context is registered.</summary>
    public List<Type> IncludedTypes { get; } = [];

    /// <summary>Entity types left out of registration (for example xref or junction tables).</summary>
    public HashSet<Type> ExcludedTypes { get; init; } = [];

    /// <summary>
    /// Authorization applied to every dynamic route. Required: mapping throws when this is null. Set <see cref="EndpointAuth.Anonymous" /> to expose the routes without
    /// authentication on purpose.
    /// </summary>
    public EndpointAuth? Auth { get; set; }

    /// <summary>Field names rejected in select, filter, sort, include, and join paths on the dynamic query routes.</summary>
    public List<string> DeniedSelectFields { get; } = [];

    /// <summary>Request-shape limits enforced on the dynamic query routes. Falls back to <see cref="DynamicEndpointDefaults.QueryPolicy" />.</summary>
    public QueryPolicy QueryPolicy { get; set; } = new DynamicEndpointDefaults().QueryPolicy;

    /// <summary>Requires an authenticated user on each dynamic route.</summary>
    public DynamicEndpointOptions<TContext> RequireAuthorization()
    {
        Auth = EndpointAuth.RequireAuthorization();
        return this;
    }

    /// <summary>Requires the named policies on each dynamic route.</summary>
    public DynamicEndpointOptions<TContext> RequireAuthorization(params string[] policyNames)
    {
        Auth = EndpointAuth.RequireAuthorization(policyNames);
        return this;
    }

    /// <summary>Opts each dynamic route out of authorization, including delete, upsert, and the property-reflecting metadata routes.</summary>
    public DynamicEndpointOptions<TContext> AllowAnonymous()
    {
        Auth = EndpointAuth.Anonymous();
        return this;
    }

    /// <summary>Leave an entity type out of registration.</summary>
    public DynamicEndpointOptions<TContext> Exclude<TEntity>()
        where TEntity : class
    {
        ExcludedTypes.Add(typeof(TEntity));
        return this;
    }

    /// <summary>Only register CRUD for these entity types (whitelist). When null or empty, every entity is considered.</summary>
    public DynamicEndpointOptions<TContext> IncludeOnly(params Type[] types)
    {
        IncludedTypes.AddRange(types);
        return this;
    }

    /// <summary>Only register CRUD for this entity type (whitelist). Chain to add more.</summary>
    public DynamicEndpointOptions<TContext> IncludeOnly<TEntity>()
        where TEntity : class
    {
        IncludedTypes.Add(typeof(TEntity));
        return this;
    }

    /// <summary>Only register CRUD for this set of entity types (whitelist).</summary>
    public DynamicEndpointOptions<TContext> IncludeOnly<T1, T2>()
        where T1 : class where T2 : class
    {
        IncludedTypes.AddRange([typeof(T1), typeof(T2)]);
        return this;
    }

    /// <summary>Only register CRUD for the three given entity types (whitelist).</summary>
    public DynamicEndpointOptions<TContext> IncludeOnly<T1, T2, T3>()
        where T1 : class where T2 : class where T3 : class
    {
        IncludedTypes.AddRange([typeof(T1), typeof(T2), typeof(T3)]);
        return this;
    }

    /// <summary>Only register CRUD for the four given entity types (whitelist).</summary>
    public DynamicEndpointOptions<TContext> IncludeOnly<T1, T2, T3, T4>()
        where T1 : class where T2 : class where T3 : class where T4 : class
    {
        IncludedTypes.AddRange([typeof(T1), typeof(T2), typeof(T3), typeof(T4)]);
        return this;
    }

    /// <summary>Only register CRUD for the five given entity types (whitelist).</summary>
    public DynamicEndpointOptions<TContext> IncludeOnly<T1, T2, T3, T4, T5>()
        where T1 : class where T2 : class where T3 : class where T4 : class where T5 : class
    {
        IncludedTypes.AddRange([typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5)]);
        return this;
    }
}