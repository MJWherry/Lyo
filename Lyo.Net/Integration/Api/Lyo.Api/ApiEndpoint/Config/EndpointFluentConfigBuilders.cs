using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.ApiEndpoint.Config;

/// <summary>Fluent config for <see cref="ApiEndpointBuilder{TDbContext,TDbEntity,TRequest,TResponse,TKey}.WithCreate"/>.</summary>
public sealed class CreateEndpointConfigBuilder<TRequest, TEntity, TDbContext>
    where TDbContext : DbContext where TEntity : class
{
    public Action<CreateContext<TRequest, TEntity, TDbContext>>? BeforeAction { get; private set; }

    public Action<CreateContext<TRequest, TEntity, TDbContext>>? AfterAction { get; private set; }

    public EndpointAuth? AuthPolicy { get; private set; }

    public CreateEndpointConfigBuilder<TRequest, TEntity, TDbContext> Before(Action<CreateContext<TRequest, TEntity, TDbContext>> before)
    {
        BeforeAction = before;
        return this;
    }

    public CreateEndpointConfigBuilder<TRequest, TEntity, TDbContext> After(Action<CreateContext<TRequest, TEntity, TDbContext>> after)
    {
        AfterAction = after;
        return this;
    }

    public CreateEndpointConfigBuilder<TRequest, TEntity, TDbContext> Auth(EndpointAuth auth)
    {
        AuthPolicy = auth;
        return this;
    }

    public CreateConfig<TRequest, TEntity, TDbContext> Build()
        => new() { Before = BeforeAction, After = AfterAction, Auth = AuthPolicy };
}

/// <summary>Fluent config for <see cref="ApiEndpointBuilder{TDbContext,TDbEntity,TRequest,TResponse,TKey}.WithGet"/>.</summary>
public sealed class GetEndpointConfigBuilder<TEntity, TDbContext>
    where TDbContext : DbContext where TEntity : class
{
    public Action<GetContext<TEntity, TDbContext>>? BeforeAction { get; private set; }

    public Action<GetContext<TEntity, TDbContext>>? AfterAction { get; private set; }

    public EndpointAuth? AuthPolicy { get; private set; }

    public GetEndpointConfigBuilder<TEntity, TDbContext> Before(Action<GetContext<TEntity, TDbContext>> before)
    {
        BeforeAction = before;
        return this;
    }

    public GetEndpointConfigBuilder<TEntity, TDbContext> After(Action<GetContext<TEntity, TDbContext>> after)
    {
        AfterAction = after;
        return this;
    }

    public GetEndpointConfigBuilder<TEntity, TDbContext> Auth(EndpointAuth auth)
    {
        AuthPolicy = auth;
        return this;
    }

    public GetConfig<TEntity, TDbContext> Build()
        => new() { Before = BeforeAction, After = AfterAction, Auth = AuthPolicy };
}

/// <summary>Fluent config for <see cref="ApiEndpointBuilder{TDbContext,TDbEntity,TRequest,TResponse,TKey}.WithUpdate"/>.</summary>
public sealed class UpdateEndpointConfigBuilder<TRequest, TEntity, TDbContext>
    where TDbContext : DbContext where TEntity : class
{
    public Action<UpdateContext<TRequest, TEntity, TDbContext>>? BeforeAction { get; private set; }

    public Action<UpdateContext<TRequest, TEntity, TDbContext>>? AfterAction { get; private set; }

    public EndpointAuth? AuthPolicy { get; private set; }

    public UpdateEndpointConfigBuilder<TRequest, TEntity, TDbContext> Before(Action<UpdateContext<TRequest, TEntity, TDbContext>> before)
    {
        BeforeAction = before;
        return this;
    }

    public UpdateEndpointConfigBuilder<TRequest, TEntity, TDbContext> After(Action<UpdateContext<TRequest, TEntity, TDbContext>> after)
    {
        AfterAction = after;
        return this;
    }

    public UpdateEndpointConfigBuilder<TRequest, TEntity, TDbContext> Auth(EndpointAuth auth)
    {
        AuthPolicy = auth;
        return this;
    }

    public UpdateConfig<TRequest, TEntity, TDbContext> Build()
        => new() { Before = BeforeAction, After = AfterAction, Auth = AuthPolicy };
}

/// <summary>Fluent config for <see cref="ApiEndpointBuilder{TDbContext,TDbEntity,TRequest,TResponse,TKey}.WithPatch"/>.</summary>
public sealed class PatchEndpointConfigBuilder<TEntity, TDbContext>
    where TDbContext : DbContext where TEntity : class
{
    public Action<PatchContext<TEntity, TDbContext>>? BeforeAction { get; private set; }

    public Action<PatchContext<TEntity, TDbContext>>? AfterAction { get; private set; }

    public EndpointAuth? AuthPolicy { get; private set; }

    public PatchPropertyAuthorization? PropertyRules { get; private set; }

    public bool InheritUpdate { get; private set; } = true;

    public PatchEndpointConfigBuilder<TEntity, TDbContext> Before(Action<PatchContext<TEntity, TDbContext>> before)
    {
        BeforeAction = before;
        return this;
    }

    public PatchEndpointConfigBuilder<TEntity, TDbContext> After(Action<PatchContext<TEntity, TDbContext>> after)
    {
        AfterAction = after;
        return this;
    }

    public PatchEndpointConfigBuilder<TEntity, TDbContext> Auth(EndpointAuth auth)
    {
        AuthPolicy = auth;
        return this;
    }

    public PatchEndpointConfigBuilder<TEntity, TDbContext> PropertyAuthorization(PatchPropertyAuthorization authorization)
    {
        PropertyRules = authorization;
        return this;
    }

    public PatchEndpointConfigBuilder<TEntity, TDbContext> PropertyAuthorization(Action<PatchPropertyAuthorizationBuilder> configure)
    {
        PropertyRules = PatchPropertyAuthorization.ForPolicies(configure);
        return this;
    }

    public PatchEndpointConfigBuilder<TEntity, TDbContext> InheritFromUpdate(bool inherit = true)
    {
        InheritUpdate = inherit;
        return this;
    }

    public PatchConfig<TEntity, TDbContext> Build()
        => new() { Before = BeforeAction, After = AfterAction, Auth = AuthPolicy, PropertyAuthorization = PropertyRules };
}

/// <summary>Fluent config for <see cref="ApiEndpointBuilder{TDbContext,TDbEntity,TRequest,TResponse,TKey}.WithDelete"/>.</summary>
public sealed class DeleteEndpointConfigBuilder<TEntity, TDbContext>
    where TDbContext : DbContext where TEntity : class
{
    public Action<DeleteContext<TEntity, TDbContext>>? BeforeAction { get; private set; }

    public Action<DeleteContext<TEntity, TDbContext>>? AfterAction { get; private set; }

    public string[]? IncludeGraph { get; private set; }

    public EndpointAuth? AuthPolicy { get; private set; }

    public DeleteEndpointConfigBuilder<TEntity, TDbContext> Before(Action<DeleteContext<TEntity, TDbContext>> before)
    {
        BeforeAction = before;
        return this;
    }

    public DeleteEndpointConfigBuilder<TEntity, TDbContext> After(Action<DeleteContext<TEntity, TDbContext>> after)
    {
        AfterAction = after;
        return this;
    }

    public DeleteEndpointConfigBuilder<TEntity, TDbContext> Includes(params string[] includes)
    {
        IncludeGraph = includes;
        return this;
    }

    public DeleteEndpointConfigBuilder<TEntity, TDbContext> Auth(EndpointAuth auth)
    {
        AuthPolicy = auth;
        return this;
    }

    public DeleteConfig<TEntity, TDbContext> Build()
        => new() { Before = BeforeAction, After = AfterAction, Includes = IncludeGraph, Auth = AuthPolicy };
}

/// <summary>Fluent config for <see cref="ApiEndpointBuilder{TDbContext,TDbEntity,TRequest,TResponse,TKey}.WithUpsert"/>.</summary>
public sealed class UpsertEndpointConfigBuilder<TRequest, TEntity, TDbContext>
    where TDbContext : DbContext where TEntity : class
{
    public Action<UpsertContext<TRequest, TEntity, TDbContext>>? BeforeUpsertAction { get; private set; }

    public Action<UpsertContext<TRequest, TEntity, TDbContext>>? AfterUpsertAction { get; private set; }

    public Action<CreateContext<TRequest, TEntity, TDbContext>>? BeforeCreateAction { get; private set; }

    public Action<CreateContext<TRequest, TEntity, TDbContext>>? AfterCreateAction { get; private set; }

    public Action<UpdateContext<TRequest, TEntity, TDbContext>>? BeforeUpdateAction { get; private set; }

    public Action<UpdateContext<TRequest, TEntity, TDbContext>>? AfterUpdateAction { get; private set; }

    public bool InheritCreate { get; private set; } = true;

    public bool InheritUpdate { get; private set; } = true;

    public EndpointAuth? AuthPolicy { get; private set; }

    /// <summary>When set, used for bulk upsert only; otherwise <see cref="AuthPolicy"/> applies to both single and bulk.</summary>
    public EndpointAuth? BulkAuthPolicy { get; private set; }

    public UpsertEndpointConfigBuilder<TRequest, TEntity, TDbContext> Before(Action<UpsertContext<TRequest, TEntity, TDbContext>> before)
    {
        BeforeUpsertAction = before;
        return this;
    }

    public UpsertEndpointConfigBuilder<TRequest, TEntity, TDbContext> After(Action<UpsertContext<TRequest, TEntity, TDbContext>> after)
    {
        AfterUpsertAction = after;
        return this;
    }

    public UpsertEndpointConfigBuilder<TRequest, TEntity, TDbContext> BeforeCreate(Action<CreateContext<TRequest, TEntity, TDbContext>> before)
    {
        BeforeCreateAction = before;
        return this;
    }

    public UpsertEndpointConfigBuilder<TRequest, TEntity, TDbContext> AfterCreate(Action<CreateContext<TRequest, TEntity, TDbContext>> after)
    {
        AfterCreateAction = after;
        return this;
    }

    public UpsertEndpointConfigBuilder<TRequest, TEntity, TDbContext> BeforeUpdate(Action<UpdateContext<TRequest, TEntity, TDbContext>> before)
    {
        BeforeUpdateAction = before;
        return this;
    }

    public UpsertEndpointConfigBuilder<TRequest, TEntity, TDbContext> AfterUpdate(Action<UpdateContext<TRequest, TEntity, TDbContext>> after)
    {
        AfterUpdateAction = after;
        return this;
    }

    public UpsertEndpointConfigBuilder<TRequest, TEntity, TDbContext> InheritFromCreate(bool inherit = true)
    {
        InheritCreate = inherit;
        return this;
    }

    public UpsertEndpointConfigBuilder<TRequest, TEntity, TDbContext> InheritFromUpdate(bool inherit = true)
    {
        InheritUpdate = inherit;
        return this;
    }

    public UpsertEndpointConfigBuilder<TRequest, TEntity, TDbContext> Auth(EndpointAuth auth)
    {
        AuthPolicy = auth;
        return this;
    }

    public UpsertEndpointConfigBuilder<TRequest, TEntity, TDbContext> BulkAuth(EndpointAuth auth)
    {
        BulkAuthPolicy = auth;
        return this;
    }
}

/// <summary>Fluent options for <see cref="ApiEndpointBuilder{TDbContext,TDbEntity,TRequest,TResponse,TKey}.WithQuery"/> (group name and default order are set by the endpoint builder).</summary>
public sealed class QueryEndpointConfigBuilder<TDbEntity>
{
    public EndpointAuth? AuthPolicy { get; private set; }

    public bool EnableComputedFields { get; private set; }

    public QueryEndpointConfigBuilder<TDbEntity> Auth(EndpointAuth auth)
    {
        AuthPolicy = auth;
        return this;
    }

    /// <summary>Enables SmartFormat computed fields on QueryProject (same as <c>WithProjectionComputedFields()</c> after query is registered).</summary>
    public QueryEndpointConfigBuilder<TDbEntity> ComputedFields(bool enable = true)
    {
        EnableComputedFields = enable;
        return this;
    }
}

/// <summary>Fluent options for <see cref="ApiEndpointBuilder{TDbContext,TDbEntity,TRequest,TResponse,TKey}.WithExport"/>.</summary>
public sealed class ExportEndpointConfigBuilder<TDbEntity>
{
    public EndpointAuth? AuthPolicy { get; private set; }

    public ExportEndpointConfigBuilder<TDbEntity> Auth(EndpointAuth auth)
    {
        AuthPolicy = auth;
        return this;
    }
}

/// <summary>Fluent config for <see cref="ApiEndpointBuilder{TDbContext,TDbEntity,TRequest,TResponse,TKey}.WithQueryHistory"/>.</summary>
public sealed class QueryHistoryEndpointConfigBuilder<TDbEntity>
{
    public Expression<Func<TDbEntity, DateTime>>? StartTime { get; private set; }

    public Expression<Func<TDbEntity, DateTime>>? EndTime { get; private set; }

    public EndpointAuth? AuthPolicy { get; private set; }

    public QueryHistoryEndpointConfigBuilder<TDbEntity> TimeRange(
        Expression<Func<TDbEntity, DateTime>> startTimeSelector,
        Expression<Func<TDbEntity, DateTime>> endTimeSelector)
    {
        StartTime = startTimeSelector;
        EndTime = endTimeSelector;
        return this;
    }

    public QueryHistoryEndpointConfigBuilder<TDbEntity> Auth(EndpointAuth auth)
    {
        AuthPolicy = auth;
        return this;
    }

    internal void Validate()
    {
        if (StartTime == null || EndTime == null)
            throw new InvalidOperationException("Call TimeRange(start, end) on the query history builder.");
    }
}

/// <summary>Fluent config for <see cref="ApiEndpointBuilder{TDbContext,TDbEntity,TRequest,TResponse,TKey}.WithMetadata"/>.</summary>
public sealed class MetadataEndpointConfigBuilder<TDbContext, TDbEntity>
    where TDbContext : DbContext where TDbEntity : class
{
    public MetadataConfiguration<TDbContext, TDbEntity> Options { get; private set; } = new();

    public EndpointAuth? AuthPolicy { get; private set; }

    public MetadataEndpointConfigBuilder<TDbContext, TDbEntity> IncludeEntityMetadata(bool include = true)
    {
        Options = Options with { IncludeEntityMetadata = include };
        return this;
    }

    public MetadataEndpointConfigBuilder<TDbContext, TDbEntity> Configuration(MetadataConfiguration<TDbContext, TDbEntity> configuration)
    {
        Options = configuration;
        return this;
    }

    public MetadataEndpointConfigBuilder<TDbContext, TDbEntity> Auth(EndpointAuth auth)
    {
        AuthPolicy = auth;
        return this;
    }
}
