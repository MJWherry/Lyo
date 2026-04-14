using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.ApiEndpoint.Dynamic;

/// <summary>Fluent builder for dynamic CRUD endpoint configuration. Configure defaults, then per-entity overrides.</summary>
/// <example>
/// <code>
/// var config = new DynamicEndpointConfigBuilder&lt;PeopleDbContext&gt;()
///     .WithDefaults(d => {
///         d.Features = ApiFeatureFlag.All;
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

    /// <summary>Configure default settings applied to all entities.</summary>
    public DynamicEndpointConfigBuilder<TContext> WithDefaults(Action<DynamicEndpointDefaults> configure)
    {
        configure(_defaults);
        return this;
    }

    /// <summary>Configure a specific entity type. Overrides merge with defaults.</summary>
    public DynamicEndpointConfigBuilder<TContext> For<TEntity>(Action<EntityEndpointConfigBuilder<TEntity, TContext>> configure)
        where TEntity : class
    {
        var builder = new EntityEndpointConfigBuilder<TEntity, TContext>(_defaults.Features);
        configure(builder);
        _entityConfigs[typeof(TEntity)] = builder.Build();
        return this;
    }

    /// <summary>Exclude entity types from registration.</summary>
    public DynamicEndpointConfigBuilder<TContext> Exclude<TEntity>()
        where TEntity : class
    {
        _defaults.ExcludedTypes.Add(typeof(TEntity));
        return this;
    }

    /// <summary>Only register these entity types (whitelist). When empty, all entities are included.</summary>
    public DynamicEndpointConfigBuilder<TContext> IncludeOnly(params Type[] types)
    {
        _defaults.IncludedTypes.Clear();
        _defaults.IncludedTypes.AddRange(types);
        return this;
    }

    /// <summary>Only register this entity type. Chain for multiple.</summary>
    public DynamicEndpointConfigBuilder<TContext> IncludeOnly<TEntity>()
        where TEntity : class
    {
        _defaults.IncludedTypes.Clear();
        _defaults.IncludedTypes.Add(typeof(TEntity));
        return this;
    }

    /// <summary>Only register these entity types.</summary>
    public DynamicEndpointConfigBuilder<TContext> IncludeOnly<T1, T2>()
        where T1 : class where T2 : class
    {
        _defaults.IncludedTypes.Clear();
        _defaults.IncludedTypes.AddRange([typeof(T1), typeof(T2)]);
        return this;
    }

    /// <summary>Only register these entity types.</summary>
    public DynamicEndpointConfigBuilder<TContext> IncludeOnly<T1, T2, T3>()
        where T1 : class where T2 : class where T3 : class
    {
        _defaults.IncludedTypes.Clear();
        _defaults.IncludedTypes.AddRange([typeof(T1), typeof(T2), typeof(T3)]);
        return this;
    }

    /// <summary>Only register these entity types.</summary>
    public DynamicEndpointConfigBuilder<TContext> IncludeOnly<T1, T2, T3, T4>()
        where T1 : class where T2 : class where T3 : class where T4 : class
    {
        _defaults.IncludedTypes.Clear();
        _defaults.IncludedTypes.AddRange([typeof(T1), typeof(T2), typeof(T3), typeof(T4)]);
        return this;
    }

    /// <summary>Build the resolved configuration.</summary>
    public DynamicEndpointConfig<TContext> Build() => new(_defaults, new Dictionary<Type, EntityEndpointConfig<TContext>>(_entityConfigs));
}