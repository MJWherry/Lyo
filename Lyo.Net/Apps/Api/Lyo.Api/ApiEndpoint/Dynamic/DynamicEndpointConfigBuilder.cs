using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.ApiEndpoint.Dynamic;

/// <summary>Fluent builder for dynamic CRUD endpoint settings. Configure defaults, then per-entity overrides.</summary>
/// <example>
/// <code>
/// var config = new DynamicEndpointConfigBuilder&lt;PeopleDbContext&gt;()
///     .WithDefaults(d => {
///         d.Features = ApiFeatureSet.DefaultCrud;
///         d.BaseRoute = "Person";
///     })
///     .For&lt;PersonEntity&gt;(e => e
///         .ExcludeCreate()
///         .ForPatch(p => p.Before((ctx, entity) => entity.ModifiedAt = DateTime.UtcNow))
///     )
///     .For&lt;AddressEntity&gt;(e => e.ExcludeExport())
///     .Build();
/// </code>
/// </example>
public sealed class DynamicEndpointConfigBuilder<TContext>
    where TContext : DbContext
{
    private readonly DynamicEndpointDefaults _defaults = new();
    private readonly Dictionary<Type, EntityEndpointConfig<TContext>> _entityConfigs = new();

    /// <summary>Configure default settings applied to every entity.</summary>
    public DynamicEndpointConfigBuilder<TContext> WithDefaults(Action<DynamicEndpointDefaults> configure)
    {
        configure(_defaults);
        return this;
    }

    /// <summary>
    /// Sets the authorization applied to every dynamic route that does not override it per operation. One of this, <see cref="RequireAuthorization()" />, or
    /// <see cref="AllowAnonymous" /> is mandatory. <see cref="Build" /> produces a config that <c>MapDynamicCrudEndpoints</c> rejects otherwise.
    /// </summary>
    public DynamicEndpointConfigBuilder<TContext> Auth(EndpointAuth auth)
    {
        _defaults.Auth = auth;
        return this;
    }

    /// <summary>Requires an authenticated user on every dynamic route that does not override that requirement.</summary>
    public DynamicEndpointConfigBuilder<TContext> RequireAuthorization() => Auth(EndpointAuth.RequireAuthorization());

    /// <summary>Requires the named policies on every dynamic route that does not override those policies.</summary>
    public DynamicEndpointConfigBuilder<TContext> RequireAuthorization(params string[] policyNames) => Auth(EndpointAuth.RequireAuthorization(policyNames));

    /// <summary>
    /// Opts every dynamic route out of authorization. Includes delete, upsert, and the metadata routes that reflect every public property of every registered entity.
    /// </summary>
    public DynamicEndpointConfigBuilder<TContext> AllowAnonymous() => Auth(EndpointAuth.Anonymous());

    /// <summary>Field names rejected in select, filter, sort, include, and join paths on the dynamic query routes.</summary>
    public DynamicEndpointConfigBuilder<TContext> DenySelectFields(params string[] fieldNames)
    {
        _defaults.DeniedSelectFields.AddRange(fieldNames);
        return this;
    }

    /// <summary>Replaces the request-shape limits enforced on the dynamic query routes. See <see cref="DynamicEndpointDefaults.QueryPolicy" /> for those defaults.</summary>
    public DynamicEndpointConfigBuilder<TContext> WithQueryPolicy(QueryPolicy policy)
    {
        ArgumentHelpers.ThrowIfNull(policy);
        _defaults.QueryPolicy = policy;
        return this;
    }

    /// <summary>Adjusts individual request-shape limits on the dynamic query routes and leaves the rest at their current values.</summary>
    public DynamicEndpointConfigBuilder<TContext> WithQueryPolicy(Func<QueryPolicy, QueryPolicy> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        _defaults.QueryPolicy = configure(_defaults.QueryPolicy);
        return this;
    }

    /// <summary>Configure one entity type. Overrides merge with defaults.</summary>
    public DynamicEndpointConfigBuilder<TContext> For<TEntity>(Action<EntityEndpointConfigBuilder<TEntity, TContext>> configure)
        where TEntity : class
    {
        var builder = new EntityEndpointConfigBuilder<TEntity, TContext>(_defaults.Features);
        configure(builder);
        _entityConfigs[typeof(TEntity)] = builder.Build();
        return this;
    }

    /// <summary>Leave entity types out of registration.</summary>
    public DynamicEndpointConfigBuilder<TContext> Exclude<TEntity>()
        where TEntity : class
    {
        _defaults.ExcludedTypes.Add(typeof(TEntity));
        return this;
    }

    /// <summary>Only register these entity types (whitelist). When empty, every entity is included.</summary>
    public DynamicEndpointConfigBuilder<TContext> IncludeOnly(params Type[] types)
    {
        _defaults.IncludedTypes.Clear();
        _defaults.IncludedTypes.AddRange(types);
        return this;
    }

    /// <summary>Only register this entity type. Chain to add more.</summary>
    public DynamicEndpointConfigBuilder<TContext> IncludeOnly<TEntity>()
        where TEntity : class
    {
        _defaults.IncludedTypes.Clear();
        _defaults.IncludedTypes.Add(typeof(TEntity));
        return this;
    }

    /// <summary>Only register this set of entity types.</summary>
    public DynamicEndpointConfigBuilder<TContext> IncludeOnly<T1, T2>()
        where T1 : class where T2 : class
    {
        _defaults.IncludedTypes.Clear();
        _defaults.IncludedTypes.AddRange([typeof(T1), typeof(T2)]);
        return this;
    }

    /// <summary>Only register the three given entity types.</summary>
    public DynamicEndpointConfigBuilder<TContext> IncludeOnly<T1, T2, T3>()
        where T1 : class where T2 : class where T3 : class
    {
        _defaults.IncludedTypes.Clear();
        _defaults.IncludedTypes.AddRange([typeof(T1), typeof(T2), typeof(T3)]);
        return this;
    }

    /// <summary>Only register the four given entity types.</summary>
    public DynamicEndpointConfigBuilder<TContext> IncludeOnly<T1, T2, T3, T4>()
        where T1 : class where T2 : class where T3 : class where T4 : class
    {
        _defaults.IncludedTypes.Clear();
        _defaults.IncludedTypes.AddRange([typeof(T1), typeof(T2), typeof(T3), typeof(T4)]);
        return this;
    }

    /// <summary>Builds the resolved settings.</summary>
    public DynamicEndpointConfig<TContext> Build() => new(_defaults, new Dictionary<Type, EntityEndpointConfig<TContext>>(_entityConfigs));
}