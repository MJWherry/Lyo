using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Lyo.Api.ApiEndpoint.Config;
using Lyo.Api.ApiEndpoint.Dynamic;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Models.Enums;
using Lyo.Api.Models.Error;
using Lyo.Api.Services.Crud.Create;
using Lyo.Api.Services.Crud.Delete;
using Lyo.Api.Services.Crud.Read;
using Lyo.Api.Services.Crud.Read.Query;
using Lyo.Api.Services.Crud.Update;
using Lyo.Api.Services.Export;
using Lyo.Common.Enums;
using Lyo.Exceptions;
using Lyo.Query.Models.Common.Request;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Api.ApiEndpoint;

public class ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey>(WebApplication app, string baseRoute, string groupName)
    where TDbContext : DbContext where TDbEntity : class
{
    private string[]? _authorizationPolicies;

    private CreateConfig<TRequest, TDbEntity, TDbContext>? _createBulkConfig;

    private CreateConfig<TRequest, TDbEntity, TDbContext>? _createConfig;

    private DeleteConfig<TDbEntity, TDbContext>? _deleteBulkConfig;

    private DeleteConfig<TDbEntity, TDbContext>? _deleteConfig;

    private ExportConfig<TDbEntity>? _exportConfig;

    private GetConfig<TDbEntity, TDbContext>? _getConfig;

    private EndpointAuth? _metadataAuth;

    private MetadataConfiguration<TDbContext, TDbEntity> _metadataConfig = new();

    private bool _metadataEnabled;

    private PatchConfig<TDbEntity, TDbContext>? _patchBulkConfig;

    private PatchConfig<TDbEntity, TDbContext>? _patchConfig;

    private QueryConfig<TDbEntity>? _queryConfig;

    private QueryHistoryConfig<TDbEntity>? _queryHistoryConfig;

    private bool _requireAuthorization;

    private UpdateConfig<TRequest, TDbEntity, TDbContext>? _updateBulkConfig;

    private UpdateConfig<TRequest, TDbEntity, TDbContext>? _updateConfig;

    private UpsertConfig<TRequest, TDbEntity, TDbContext>? _upsertBulkConfig;

    private UpsertConfig<TRequest, TDbEntity, TDbContext>? _upsertConfig;

    /// <summary>Requires that all endpoints built by this builder use the default authorization policy (authenticated user).</summary>
    /// <example>
    /// <code>app.CreateBuilder&lt;...&gt;("/api/items", "Items").RequireAuthorization().WithCrud(...).Build();</code>
    /// </example>
    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> RequireAuthorization()
    {
        _requireAuthorization = true;
        _authorizationPolicies = null;
        return this;
    }

    /// <summary>Requires that all endpoints built by this builder use the specified authorization policy or policies.</summary>
    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> RequireAuthorization(params string[] policyNames)
    {
        _requireAuthorization = false;
        _authorizationPolicies = policyNames;
        return this;
    }

    /// <summary>Marks all endpoints built by this builder as allowing anonymous access (no authentication required).</summary>
    /// <example>
    /// <code>app.CreateBuilder&lt;...&gt;("/api/public/items", "Public").AllowAnonymous().WithReadOnlyEndpoints(...).Build();</code>
    /// </example>
    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> AllowAnonymous()
    {
        _requireAuthorization = false;
        _authorizationPolicies = null;
        return this;
    }

    private IEndpointConventionBuilder ApplyAuthorization(IEndpointConventionBuilder builder, EndpointAuth? endpointAuth)
    {
        if (endpointAuth != null) {
            EndpointAuth.Validate(endpointAuth);
            if (endpointAuth.AllowAnonymous)
                return builder.AllowAnonymous();

            if (endpointAuth.AuthorizationPolicy != null)
                return builder.RequireAuthorization(endpointAuth.AuthorizationPolicy);

            if (endpointAuth.AuthorizationPolicies is { Length: > 0 })
                return builder.RequireAuthorization(endpointAuth.AuthorizationPolicies);

            return builder.RequireAuthorization();
        }

        if (_authorizationPolicies is { Length: > 0 })
            return builder.RequireAuthorization(_authorizationPolicies);

        if (_requireAuthorization)
            return builder.RequireAuthorization();

        return builder;
    }

    private Expression<Func<TDbEntity, object?>> ResolveDefaultOrderFromPrimaryKey()
    {
        using var scope = app.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TDbContext>>();
        using var context = factory.CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(TDbEntity));
        OperationHelpers.ThrowIfNull(entityType, $"Entity type {typeof(TDbEntity).Name} not found in model.");
        var pk = entityType.FindPrimaryKey();
        OperationHelpers.ThrowIfNull(pk, $"Entity {typeof(TDbEntity).Name} has no primary key.");
        if (pk.Properties.Count == 0)
            throw new InvalidOperationException($"Entity {typeof(TDbEntity).Name} has an empty primary key.");

        // Single- or composite-key: default query/export sort uses the first key property (EF key order).
        return DynamicEndpointMapper.BuildDefaultOrderExpression<TDbEntity>(pk.Properties[0].Name);
    }

    private (string Name, Type ClrType) ResolvePrimaryKeyMetadata()
    {
        using var scope = app.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TDbContext>>();
        using var context = factory.CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(TDbEntity));
        OperationHelpers.ThrowIfNull(entityType, $"Entity type {typeof(TDbEntity).Name} not found in model.");
        var pk = entityType.FindPrimaryKey();
        OperationHelpers.ThrowIfNull(pk, $"Entity {typeof(TDbEntity).Name} has no primary key.");
        if (pk.Properties.Count == 0)
            throw new InvalidOperationException($"Entity {typeof(TDbEntity).Name} has an empty primary key.");

        return (pk.Properties[0].Name, pk.Properties[0].ClrType);
    }

    private EndpointMetadataResponse BuildMetadataResponse()
    {
        var nullability = new NullabilityInfoContext();

        PropertyMetadata ToPropertyMetadata(PropertyInfo property)
        {
            var propertyType = property.PropertyType;
            var underlying = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            var propertyNullability = nullability.Create(property);
            var isNullable = propertyNullability.ReadState == NullabilityState.Nullable || Nullable.GetUnderlyingType(propertyType) != null ||
                (!propertyType.IsValueType && propertyNullability.ReadState == NullabilityState.Unknown);

            return new(property.Name, underlying.GetFriendlyTypeName(), isNullable);
        }

        TypeMetadata ToTypeMetadata(Type type)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .Select(ToPropertyMetadata)
                .OrderBy(p => p.Name, StringComparer.Ordinal)
                .ToList();

            return new(type.Name, properties);
        }

        var (keyPropertyName, keyType) = ResolvePrimaryKeyMetadata();
        return new(
            _metadataConfig.IncludeEntityMetadata ? ToTypeMetadata(typeof(TDbEntity)) : null, typeof(TRequest) == typeof(object) ? null : ToTypeMetadata(typeof(TRequest)),
            typeof(TResponse) == typeof(object) ? null : ToTypeMetadata(typeof(TResponse)), keyPropertyName, keyType.Name);
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithCrud(ApiFeatureFlag features, CrudConfiguration<TDbContext, TDbEntity, TRequest> config)
    {
        if (features.HasFlag(ApiFeatureFlag.Query)) {
            WithQuery(config.QueryAuth);
            if (features.HasFlag(ApiFeatureFlag.ProjectionComputedFields))
                WithProjectionComputedFields();
        }

        if (features.HasFlag(ApiFeatureFlag.Get))
            WithGet(config.BeforeGet, config.AfterGet, config.GetAuth);

        if (features.HasFlag(ApiFeatureFlag.Create))
            WithCreate(config.BeforeCreate, config.AfterCreate, config.CreateAuth);

        if (features.HasFlag(ApiFeatureFlag.CreateBulk))
            WithCreateBulk(config.BeforeCreate, config.AfterCreate, config.CreateBulkAuth);

        if (features.HasFlag(ApiFeatureFlag.Update))
            WithUpdate(config.BeforeUpdate, config.AfterUpdate, config.UpdateAuth);

        if (features.HasFlag(ApiFeatureFlag.UpdateBulk))
            WithUpdateBulk(config.BeforeUpdate, config.AfterUpdate, config.UpdateBulkAuth);

        if (features.HasFlag(ApiFeatureFlag.Patch)) {
            var beforePatch = config.BeforePatch;
            var afterPatch = config.AfterPatch;
            if (features.HasFlag(ApiFeatureFlag.PatchInheritsUpdate) && (config.BeforeUpdate != null || config.AfterUpdate != null)) {
                beforePatch ??= config.BeforeUpdate != null
                    ? ctx => config.BeforeUpdate!(new(new() { Keys = ctx.Request.Keys?.FirstOrDefault() ?? [], Data = default! }, ctx.Entity, ctx.DbContext, ctx.Services))
                    : null;

                afterPatch ??= config.AfterUpdate != null
                    ? ctx => config.AfterUpdate!(new(new() { Keys = ctx.Request.Keys?.FirstOrDefault() ?? [], Data = default! }, ctx.Entity, ctx.DbContext, ctx.Services))
                    : null;
            }

            WithPatch(beforePatch, afterPatch, true, config.PatchAuth, config.PatchPropertyAuthorization);
        }

        if (features.HasFlag(ApiFeatureFlag.PatchBulk)) {
            var beforePatch = config.BeforePatch;
            var afterPatch = config.AfterPatch;
            if (features.HasFlag(ApiFeatureFlag.PatchInheritsUpdate) && (config.BeforeUpdate != null || config.AfterUpdate != null)) {
                beforePatch ??= config.BeforeUpdate != null
                    ? ctx => config.BeforeUpdate!(new(new() { Keys = ctx.Request.Keys?.FirstOrDefault() ?? [], Data = default! }, ctx.Entity, ctx.DbContext, ctx.Services))
                    : null;

                afterPatch ??= config.AfterUpdate != null
                    ? ctx => config.AfterUpdate!(new(new() { Keys = ctx.Request.Keys?.FirstOrDefault() ?? [], Data = default! }, ctx.Entity, ctx.DbContext, ctx.Services))
                    : null;
            }

            WithPatchBulk(beforePatch, afterPatch, true, config.PatchBulkAuth, config.PatchPropertyAuthorization);
        }

        if (features.HasFlag(ApiFeatureFlag.Delete))
            WithDelete(config.BeforeDelete, config.AfterDelete, config.DeleteIncludes, config.DeleteAuth);

        if (features.HasFlag(ApiFeatureFlag.DeleteBulk))
            WithDeleteBulk(config.BeforeDelete, config.AfterDelete, config.DeleteIncludes, null, config.DeleteBulkAuth);

        if (features.HasFlag(ApiFeatureFlag.Export))
            WithExport(config.ExportAuth);

        if (features.HasFlag(ApiFeatureFlag.Metadata))
            WithMetadata(config.Metadata, config.MetadataAuth);

        if (features.HasFlag(ApiFeatureFlag.Upsert)) {
            var beforeCreate = config.BeforeCreate;
            var afterCreate = config.AfterCreate;
            var beforeUpdate = config.BeforeUpdate;
            var afterUpdate = config.AfterUpdate;
            if (features.HasFlag(ApiFeatureFlag.UpsertInheritCreate)) {
                beforeCreate = config.BeforeCreate;
                afterCreate = config.AfterCreate;
            }

            if (features.HasFlag(ApiFeatureFlag.UpsertInheritUpdate)) {
                beforeUpdate = config.BeforeUpdate;
                afterUpdate = config.AfterUpdate;
            }

            WithUpsert(
                config.BeforeUpsert, config.AfterUpsert, beforeCreate, afterCreate, beforeUpdate, afterUpdate, features.HasFlag(ApiFeatureFlag.UpsertInheritCreate),
                features.HasFlag(ApiFeatureFlag.UpsertInheritUpdate), config.UpsertAuth);
        }

        if (features.HasFlag(ApiFeatureFlag.UpsertBulk)) {
            var beforeCreate = config.BeforeCreate;
            var afterCreate = config.AfterCreate;
            var beforeUpdate = config.BeforeUpdate;
            var afterUpdate = config.AfterUpdate;
            if (features.HasFlag(ApiFeatureFlag.UpsertInheritCreate)) {
                beforeCreate = config.BeforeCreate;
                afterCreate = config.AfterCreate;
            }

            if (features.HasFlag(ApiFeatureFlag.UpsertInheritUpdate)) {
                beforeUpdate = config.BeforeUpdate;
                afterUpdate = config.AfterUpdate;
            }

            WithUpsertBulk(
                config.BeforeUpsert, config.AfterUpsert, beforeCreate, afterCreate, beforeUpdate, afterUpdate, features.HasFlag(ApiFeatureFlag.UpsertInheritCreate),
                features.HasFlag(ApiFeatureFlag.UpsertInheritUpdate), config.UpsertBulkAuth);
        }

        return this;
    }

    /// <summary>Fluent CRUD setup: <c>WithCrud(crud =&gt; crud.WithFlags(flags).BeforeCreate(...))</c>.</summary>
    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithCrud(
        Action<CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest>();
        configure(builder);
        var (features, config) = builder.Build();
        return WithCrud(features, config);
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithQuery(EndpointAuth? auth = null)
    {
        _queryConfig = new() { GroupName = groupName, DefaultOrder = ResolveDefaultOrderFromPrimaryKey(), Auth = auth };
        return this;
    }

    /// <summary>Enables computed fields (SmartFormat templates) on the QueryProject endpoint. Requires WithQuery and IFormatterService.</summary>
    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithProjectionComputedFields()
    {
        if (_queryConfig == null)
            throw new InvalidOperationException("WithProjectionComputedFields requires WithQuery to be called first.");

        _queryConfig = _queryConfig with { EnableComputedFields = true };
        return this;
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithExport(EndpointAuth? auth = null)
    {
        _exportConfig = new() { GroupName = groupName, DefaultOrder = ResolveDefaultOrderFromPrimaryKey(), Auth = auth };
        return this;
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithMetadata(EndpointAuth? auth = null) => WithMetadata(new(), auth);

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithMetadata(MetadataConfiguration<TDbContext, TDbEntity> config, EndpointAuth? auth = null)
    {
        _metadataEnabled = true;
        _metadataConfig = config;
        _metadataAuth = auth;
        return this;
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithQueryHistory(
        Expression<Func<TDbEntity, DateTime>> startTimeSelector,
        Expression<Func<TDbEntity, DateTime>> endTimeSelector,
        EndpointAuth? auth = null)
    {
        _queryHistoryConfig = new() {
            GroupName = groupName,
            DefaultOrder = ResolveDefaultOrderFromPrimaryKey(),
            StartTimeSelector = startTimeSelector,
            EndTimeSelector = endTimeSelector,
            Auth = auth
        };

        return this;
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithGet(
        Action<GetContext<TDbEntity, TDbContext>>? before = null,
        Action<GetContext<TDbEntity, TDbContext>>? after = null,
        EndpointAuth? auth = null)
    {
        _getConfig = new() { Before = before, After = after, Auth = auth };
        return this;
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithCreate(
        Action<CreateContext<TRequest, TDbEntity, TDbContext>>? before = null,
        Action<CreateContext<TRequest, TDbEntity, TDbContext>>? after = null,
        EndpointAuth? auth = null)
    {
        _createConfig = new() { Before = before, After = after, Auth = auth };
        return this;
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithCreateBulk(
        Action<CreateContext<TRequest, TDbEntity, TDbContext>>? before = null,
        Action<CreateContext<TRequest, TDbEntity, TDbContext>>? after = null,
        EndpointAuth? auth = null)
    {
        _createBulkConfig = new() { Before = before, After = after, Auth = auth };
        return this;
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithUpdate(
        Action<UpdateContext<TRequest, TDbEntity, TDbContext>>? before = null,
        Action<UpdateContext<TRequest, TDbEntity, TDbContext>>? after = null,
        EndpointAuth? auth = null)
    {
        _updateConfig = new() { Before = before, After = after, Auth = auth };
        return this;
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithUpdateBulk(
        Action<UpdateContext<TRequest, TDbEntity, TDbContext>>? before = null,
        Action<UpdateContext<TRequest, TDbEntity, TDbContext>>? after = null,
        EndpointAuth? auth = null)
    {
        _updateBulkConfig = new() { Before = before, After = after, Auth = auth };
        return this;
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithPatch(
        Action<PatchContext<TDbEntity, TDbContext>>? before = null,
        Action<PatchContext<TDbEntity, TDbContext>>? after = null,
        bool inheritUpdate = true,
        EndpointAuth? auth = null,
        PatchPropertyAuthorization? propertyAuthorization = null)
    {
        Action<PatchContext<TDbEntity, TDbContext>>? inheritedBefore = null;
        Action<PatchContext<TDbEntity, TDbContext>>? inheritedAfter = null;
        if (_updateConfig?.Before != null && inheritUpdate) {
            inheritedBefore = ctx
                => _updateConfig.Before!(new(new() { Keys = ctx.Request.Keys?.FirstOrDefault() ?? [], Data = default! }, ctx.Entity, ctx.DbContext, ctx.Services));
        }

        if (_updateConfig?.After != null && inheritUpdate)
            inheritedAfter = ctx => _updateConfig.After!(new(new() { Keys = ctx.Request.Keys?.FirstOrDefault() ?? [], Data = default! }, ctx.Entity, ctx.DbContext, ctx.Services));

        _patchConfig = new() {
            Before = before ?? inheritedBefore, After = after ?? inheritedAfter, Auth = auth, PropertyAuthorization = propertyAuthorization
        };
        return this;
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithPatchBulk(
        Action<PatchContext<TDbEntity, TDbContext>>? before = null,
        Action<PatchContext<TDbEntity, TDbContext>>? after = null,
        bool inheritUpdate = true,
        EndpointAuth? auth = null,
        PatchPropertyAuthorization? propertyAuthorization = null)
    {
        Action<PatchContext<TDbEntity, TDbContext>>? inheritedBefore = null;
        Action<PatchContext<TDbEntity, TDbContext>>? inheritedAfter = null;
        if (_updateBulkConfig?.Before != null && inheritUpdate) {
            inheritedBefore = ctx
                => _updateBulkConfig.Before!(new(new() { Keys = ctx.Request.Keys?.FirstOrDefault() ?? [], Data = default! }, ctx.Entity, ctx.DbContext, ctx.Services));
        }

        if (_updateBulkConfig?.After != null && inheritUpdate) {
            inheritedAfter = ctx
                => _updateBulkConfig.After!(new(new() { Keys = ctx.Request.Keys?.FirstOrDefault() ?? [], Data = default! }, ctx.Entity, ctx.DbContext, ctx.Services));
        }

        _patchBulkConfig = new() {
            Before = before ?? inheritedBefore, After = after ?? inheritedAfter, Auth = auth, PropertyAuthorization = propertyAuthorization
        };
        return this;
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithUpsert(
        Action<UpsertContext<TRequest, TDbEntity, TDbContext>>? before = null,
        Action<UpsertContext<TRequest, TDbEntity, TDbContext>>? after = null,
        Action<CreateContext<TRequest, TDbEntity, TDbContext>>? beforeCreate = null,
        Action<CreateContext<TRequest, TDbEntity, TDbContext>>? afterCreate = null,
        Action<UpdateContext<TRequest, TDbEntity, TDbContext>>? beforeUpdate = null,
        Action<UpdateContext<TRequest, TDbEntity, TDbContext>>? afterUpdate = null,
        bool inheritCreate = true,
        bool inheritUpdate = true,
        EndpointAuth? auth = null)
    {
        _upsertConfig = new() {
            Before = before,
            After = after,
            BeforeCreate =
                beforeCreate != null ? ctx => beforeCreate(new(ctx.Request.NewData!, ctx.Entity, ctx.DbContext, ctx.Services)) :
                _createConfig?.Before != null && inheritCreate ? ctx => _createConfig.Before!(new(ctx.Request.NewData!, ctx.Entity, ctx.DbContext, ctx.Services)) : null,
            AfterCreate =
                afterCreate != null ? ctx => afterCreate(new(ctx.Request.NewData!, ctx.Entity, ctx.DbContext, ctx.Services)) :
                _createConfig?.After != null && inheritCreate ? ctx => _createConfig.After!(new(ctx.Request.NewData!, ctx.Entity, ctx.DbContext, ctx.Services)) : null,
            BeforeUpdate =
                beforeUpdate != null ? ctx => beforeUpdate(new(new() { Keys = ctx.Request.Keys ?? [], Data = ctx.Request.NewData! }, ctx.Entity, ctx.DbContext, ctx.Services)) :
                _updateConfig?.Before != null && inheritUpdate ? ctx
                    => _updateConfig.Before!(new(new() { Keys = ctx.Request.Keys ?? [], Data = ctx.Request.NewData! }, ctx.Entity, ctx.DbContext, ctx.Services)) : null,
            AfterUpdate = afterUpdate != null ? ctx
                    => afterUpdate(new(new() { Keys = ctx.Request.Keys ?? [], Data = ctx.Request.NewData! }, ctx.Entity, ctx.DbContext, ctx.Services)) :
                _updateConfig?.After != null && inheritUpdate ? ctx
                    => _updateConfig.After!(new(new() { Keys = ctx.Request.Keys ?? [], Data = ctx.Request.NewData! }, ctx.Entity, ctx.DbContext, ctx.Services)) : null,
            Auth = auth
        };

        return this;
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithUpsertBulk(
        Action<UpsertContext<TRequest, TDbEntity, TDbContext>>? before = null,
        Action<UpsertContext<TRequest, TDbEntity, TDbContext>>? after = null,
        Action<CreateContext<TRequest, TDbEntity, TDbContext>>? beforeCreate = null,
        Action<CreateContext<TRequest, TDbEntity, TDbContext>>? afterCreate = null,
        Action<UpdateContext<TRequest, TDbEntity, TDbContext>>? beforeUpdate = null,
        Action<UpdateContext<TRequest, TDbEntity, TDbContext>>? afterUpdate = null,
        bool inheritCreate = true,
        bool inheritUpdate = true,
        EndpointAuth? auth = null)
    {
        _upsertBulkConfig = new() {
            Before = before,
            After = after,
            BeforeCreate =
                beforeCreate != null ? ctx => beforeCreate(new(ctx.Request.NewData!, ctx.Entity, ctx.DbContext, ctx.Services)) :
                _createBulkConfig?.Before != null && inheritCreate ? ctx => _createBulkConfig.Before!(new(ctx.Request.NewData!, ctx.Entity, ctx.DbContext, ctx.Services)) : null,
            AfterCreate =
                afterCreate != null ? ctx => afterCreate(new(ctx.Request.NewData!, ctx.Entity, ctx.DbContext, ctx.Services)) :
                _createBulkConfig?.After != null && inheritCreate ? ctx => _createBulkConfig.After!(new(ctx.Request.NewData!, ctx.Entity, ctx.DbContext, ctx.Services)) : null,
            BeforeUpdate =
                beforeUpdate != null ? ctx => beforeUpdate(new(new() { Keys = ctx.Request.Keys ?? [], Data = ctx.Request.NewData! }, ctx.Entity, ctx.DbContext, ctx.Services)) :
                _updateBulkConfig?.Before != null && inheritUpdate ? ctx
                    => _updateBulkConfig.Before!(new(new() { Keys = ctx.Request.Keys ?? [], Data = ctx.Request.NewData! }, ctx.Entity, ctx.DbContext, ctx.Services)) : null,
            AfterUpdate = afterUpdate != null ? ctx
                    => afterUpdate(new(new() { Keys = ctx.Request.Keys ?? [], Data = ctx.Request.NewData! }, ctx.Entity, ctx.DbContext, ctx.Services)) :
                _updateBulkConfig?.After != null && inheritUpdate ? ctx
                    => _updateBulkConfig.After!(new(new() { Keys = ctx.Request.Keys ?? [], Data = ctx.Request.NewData! }, ctx.Entity, ctx.DbContext, ctx.Services)) : null,
            Auth = auth
        };

        return this;
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithDelete(
        Action<DeleteContext<TDbEntity, TDbContext>>? before = null,
        Action<DeleteContext<TDbEntity, TDbContext>>? after = null,
        string[]? includes = null,
        EndpointAuth? auth = null)
    {
        _deleteConfig = new() {
            Before = before,
            After = after,
            Includes = includes,
            Auth = auth
        };

        return this;
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithDeleteBulk(
        Action<DeleteContext<TDbEntity, TDbContext>>? before = null,
        Action<DeleteContext<TDbEntity, TDbContext>>? after = null,
        string[]? includes = null,
        string? endpoint = null,
        EndpointAuth? auth = null)
    {
        _deleteBulkConfig = new() {
            Before = before,
            After = after,
            Includes = includes,
            Auth = auth
        };

        return this;
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithQuery(QueryConfig<TDbEntity> config)
    {
        _queryConfig = config;
        return this;
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithQuery(Action<QueryEndpointConfigBuilder<TDbEntity>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var b = new QueryEndpointConfigBuilder<TDbEntity>();
        configure(b);
        _queryConfig = new() {
            GroupName = groupName, DefaultOrder = ResolveDefaultOrderFromPrimaryKey(), Auth = b.AuthPolicy, EnableComputedFields = b.EnableComputedFields
        };

        return this;
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithExport(ExportConfig<TDbEntity> config)
    {
        _exportConfig = config;
        return this;
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithExport(Action<ExportEndpointConfigBuilder<TDbEntity>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var b = new ExportEndpointConfigBuilder<TDbEntity>();
        configure(b);
        _exportConfig = new() { GroupName = groupName, DefaultOrder = ResolveDefaultOrderFromPrimaryKey(), Auth = b.AuthPolicy };
        return this;
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithQueryHistory(QueryHistoryConfig<TDbEntity> config)
    {
        _queryHistoryConfig = config;
        return this;
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithQueryHistory(Action<QueryHistoryEndpointConfigBuilder<TDbEntity>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var b = new QueryHistoryEndpointConfigBuilder<TDbEntity>();
        configure(b);
        b.Validate();
        return WithQueryHistory(b.StartTime!, b.EndTime!, b.AuthPolicy);
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithMetadata(Action<MetadataEndpointConfigBuilder<TDbContext, TDbEntity>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var metaBuilder = new MetadataEndpointConfigBuilder<TDbContext, TDbEntity>();
        configure(metaBuilder);
        return WithMetadata(metaBuilder.Options, metaBuilder.AuthPolicy);
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithGet(GetConfig<TDbEntity, TDbContext> config)
    {
        _getConfig = config;
        return this;
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithGet(Action<GetEndpointConfigBuilder<TDbEntity, TDbContext>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var b = new GetEndpointConfigBuilder<TDbEntity, TDbContext>();
        configure(b);
        return WithGet(b.Build());
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithCreate(CreateConfig<TRequest, TDbEntity, TDbContext> config)
    {
        _createConfig = config;
        return this;
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithCreate(Action<CreateEndpointConfigBuilder<TRequest, TDbEntity, TDbContext>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var b = new CreateEndpointConfigBuilder<TRequest, TDbEntity, TDbContext>();
        configure(b);
        return WithCreate(b.Build());
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithCreateBulk(CreateConfig<TRequest, TDbEntity, TDbContext> config)
    {
        _createBulkConfig = config;
        return this;
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithCreateBulk(Action<CreateEndpointConfigBuilder<TRequest, TDbEntity, TDbContext>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var b = new CreateEndpointConfigBuilder<TRequest, TDbEntity, TDbContext>();
        configure(b);
        return WithCreateBulk(b.Build());
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithUpdate(UpdateConfig<TRequest, TDbEntity, TDbContext> config)
    {
        _updateConfig = config;
        return this;
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithUpdate(Action<UpdateEndpointConfigBuilder<TRequest, TDbEntity, TDbContext>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var b = new UpdateEndpointConfigBuilder<TRequest, TDbEntity, TDbContext>();
        configure(b);
        return WithUpdate(b.Build());
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithUpdateBulk(UpdateConfig<TRequest, TDbEntity, TDbContext> config)
    {
        _updateBulkConfig = config;
        return this;
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithUpdateBulk(Action<UpdateEndpointConfigBuilder<TRequest, TDbEntity, TDbContext>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var b = new UpdateEndpointConfigBuilder<TRequest, TDbEntity, TDbContext>();
        configure(b);
        return WithUpdateBulk(b.Build());
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithPatch(PatchConfig<TDbEntity, TDbContext> config, bool inheritUpdate = true)
    {
        return WithPatch(config.Before, config.After, inheritUpdate, config.Auth, config.PropertyAuthorization);
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithPatch(Action<PatchEndpointConfigBuilder<TDbEntity, TDbContext>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var b = new PatchEndpointConfigBuilder<TDbEntity, TDbContext>();
        configure(b);
        var c = b.Build();
        return WithPatch(c.Before, c.After, b.InheritUpdate, c.Auth, c.PropertyAuthorization);
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithPatchBulk(PatchConfig<TDbEntity, TDbContext> config, bool inheritUpdate = true)
    {
        return WithPatchBulk(config.Before, config.After, inheritUpdate, config.Auth, config.PropertyAuthorization);
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithPatchBulk(Action<PatchEndpointConfigBuilder<TDbEntity, TDbContext>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var b = new PatchEndpointConfigBuilder<TDbEntity, TDbContext>();
        configure(b);
        var c = b.Build();
        return WithPatchBulk(c.Before, c.After, b.InheritUpdate, c.Auth, c.PropertyAuthorization);
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithUpsert(UpsertConfig<TRequest, TDbEntity, TDbContext> config)
    {
        _upsertConfig = config;
        return this;
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithUpsert(Action<UpsertEndpointConfigBuilder<TRequest, TDbEntity, TDbContext>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var b = new UpsertEndpointConfigBuilder<TRequest, TDbEntity, TDbContext>();
        configure(b);
        return WithUpsert(
            b.BeforeUpsertAction, b.AfterUpsertAction, b.BeforeCreateAction, b.AfterCreateAction, b.BeforeUpdateAction, b.AfterUpdateAction, b.InheritCreate, b.InheritUpdate,
            b.AuthPolicy);
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithUpsertBulk(UpsertConfig<TRequest, TDbEntity, TDbContext> config)
    {
        _upsertBulkConfig = config;
        return this;
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithUpsertBulk(Action<UpsertEndpointConfigBuilder<TRequest, TDbEntity, TDbContext>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var b = new UpsertEndpointConfigBuilder<TRequest, TDbEntity, TDbContext>();
        configure(b);
        return WithUpsertBulk(
            b.BeforeUpsertAction, b.AfterUpsertAction, b.BeforeCreateAction, b.AfterCreateAction, b.BeforeUpdateAction, b.AfterUpdateAction, b.InheritCreate, b.InheritUpdate,
            b.BulkAuthPolicy ?? b.AuthPolicy);
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithDelete(DeleteConfig<TDbEntity, TDbContext> config)
    {
        _deleteConfig = config;
        return this;
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithDelete(Action<DeleteEndpointConfigBuilder<TDbEntity, TDbContext>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var b = new DeleteEndpointConfigBuilder<TDbEntity, TDbContext>();
        configure(b);
        return WithDelete(b.Build());
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithDeleteBulk(DeleteConfig<TDbEntity, TDbContext> config)
    {
        _deleteBulkConfig = config;
        return this;
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithDeleteBulk(Action<DeleteEndpointConfigBuilder<TDbEntity, TDbContext>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var b = new DeleteEndpointConfigBuilder<TDbEntity, TDbContext>();
        configure(b);
        return WithDeleteBulk(b.Build());
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithCreateAndBulk(Action<CreateEndpointConfigBuilder<TRequest, TDbEntity, TDbContext>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var b = new CreateEndpointConfigBuilder<TRequest, TDbEntity, TDbContext>();
        configure(b);
        var c = b.Build();
        return WithCreate(c).WithCreateBulk(c);
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithUpdateAndBulk(Action<UpdateEndpointConfigBuilder<TRequest, TDbEntity, TDbContext>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var b = new UpdateEndpointConfigBuilder<TRequest, TDbEntity, TDbContext>();
        configure(b);
        var c = b.Build();
        return WithUpdate(c).WithUpdateBulk(c);
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithPatchAndBulk(Action<PatchEndpointConfigBuilder<TDbEntity, TDbContext>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var b = new PatchEndpointConfigBuilder<TDbEntity, TDbContext>();
        configure(b);
        var c = b.Build();
        return WithPatch(c.Before, c.After, b.InheritUpdate, c.Auth, c.PropertyAuthorization)
            .WithPatchBulk(c.Before, c.After, b.InheritUpdate, c.Auth, c.PropertyAuthorization);
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithUpsertAndBulk(Action<UpsertEndpointConfigBuilder<TRequest, TDbEntity, TDbContext>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var b = new UpsertEndpointConfigBuilder<TRequest, TDbEntity, TDbContext>();
        configure(b);
        return WithUpsert(
                b.BeforeUpsertAction, b.AfterUpsertAction, b.BeforeCreateAction, b.AfterCreateAction, b.BeforeUpdateAction, b.AfterUpdateAction, b.InheritCreate, b.InheritUpdate,
                b.AuthPolicy)
            .WithUpsertBulk(
                b.BeforeUpsertAction, b.AfterUpsertAction, b.BeforeCreateAction, b.AfterCreateAction, b.BeforeUpdateAction, b.AfterUpdateAction, b.InheritCreate, b.InheritUpdate,
                b.BulkAuthPolicy ?? b.AuthPolicy);
    }

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithCreateAndBulk(
        Action<CreateContext<TRequest, TDbEntity, TDbContext>>? before = null,
        Action<CreateContext<TRequest, TDbEntity, TDbContext>>? after = null)
        => WithCreate(before, after).WithCreateBulk(before, after);

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithUpdateAndBulk(
        Action<UpdateContext<TRequest, TDbEntity, TDbContext>>? before = null,
        Action<UpdateContext<TRequest, TDbEntity, TDbContext>>? after = null)
        => WithUpdate(before, after).WithUpdateBulk(before, after);

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithPatchAndBulk(
        Action<PatchContext<TDbEntity, TDbContext>>? before = null,
        Action<PatchContext<TDbEntity, TDbContext>>? after = null,
        PatchPropertyAuthorization? propertyAuthorization = null)
        => WithPatch(before, after, true, null, propertyAuthorization).WithPatchBulk(before, after, true, null, propertyAuthorization);

    public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> WithUpsertAndBulk(
        Action<UpsertContext<TRequest, TDbEntity, TDbContext>>? before = null,
        Action<UpsertContext<TRequest, TDbEntity, TDbContext>>? after = null,
        Action<CreateContext<TRequest, TDbEntity, TDbContext>>? beforeCreate = null,
        Action<CreateContext<TRequest, TDbEntity, TDbContext>>? afterCreate = null,
        Action<UpdateContext<TRequest, TDbEntity, TDbContext>>? beforeUpdate = null,
        Action<UpdateContext<TRequest, TDbEntity, TDbContext>>? afterUpdate = null,
        bool inheritCreate = true,
        bool inheritUpdate = true)
        => WithUpsert(before, after, beforeCreate, afterCreate, beforeUpdate, afterUpdate, inheritCreate, inheritUpdate)
            .WithUpsertBulk(before, after, beforeCreate, afterCreate, beforeUpdate, afterUpdate, inheritCreate, inheritUpdate);

    public void Build()
    {
        BuildMetadata();
        BuildQuery();
        BuildQueryHistory();
        BuildExport();
        BuildGet();
        BuildCreate();
        BuildCreateBulk();
        BuildUpdate();
        BuildUpdateBulk();
        BuildPatch();
        BuildPatchBulk();
        BuildUpsert();
        BuildUpsertBulk();
        BuildDelete();
        BuildDeleteBulk();
    }

    private void BuildMetadata()
    {
        if (!_metadataEnabled)
            return;

        var metadata = BuildMetadataResponse();
        var routeBuilder = app.MapGet($"{baseRoute}/Metadata", () => Results.Json(metadata))
            .WithName($"Metadata{Regex.Replace(typeof(TResponse).Name, "Res$", "")}")
            .WithTags(Regex.Replace(typeof(TResponse).Name, "Res$", ""))
            .Produces<EndpointMetadataResponse>();

        ApplyAuthorization(routeBuilder, _metadataAuth);
    }

    private void BuildQuery()
    {
        if (_queryConfig == null)
            return;

        var routeBuilder = app.MapPost(
                $"{baseRoute}/Query", async (
                    [FromBody] QueryReq queryRequest,
                    [FromServices] IQueryService<TDbContext> basicService,
                    HttpContext httpContext,
                    CancellationToken ct = default) => {
                    var result = await basicService.Query<TDbEntity, TResponse>(queryRequest, _queryConfig.DefaultOrder, SortDirection.Desc, ct).ConfigureAwait(false);
                    if (result.IsSuccess)
                        return Results.Ok(result);

                    var error = ApiErrorResponseFactory.CreateForError(httpContext, result.Error);
                    return Results.Json(error, statusCode: error.Status);
                })
            .WithName($"Query{Regex.Replace(typeof(TResponse).Name, "Res$", "")}")
            .WithTags(Regex.Replace(typeof(TResponse).Name, "Res$", ""))
            .Produces<QueryRes<TResponse>>()
            .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest);

        ApplyAuthorization(routeBuilder, _queryConfig.Auth);
        var projectedRouteBuilder = app.MapPost(
                $"{baseRoute}/QueryProject", async (
                    [FromBody] ProjectionQueryReq queryRequest,
                    [FromServices] IQueryService<TDbContext> basicService,
                    HttpContext httpContext,
                    CancellationToken ct = default) => {
                    if (!_queryConfig.EnableComputedFields && queryRequest.ComputedFields.Count > 0) {
                        var cfError = ApiErrorResponseFactory.CreateForError(
                            httpContext,
                            LyoProblemDetails.FromCode(
                                Lyo.Api.Models.Constants.ApiErrorCodes.InvalidQuery,
                                "Computed fields are not enabled. Enable via ApiFeatureFlag.ProjectionComputedFields or WithProjectionComputedFields.",
                                DateTime.UtcNow));

                        return Results.Json(cfError, statusCode: cfError.Status);
                    }

                    var result = await basicService.QueryProjected(queryRequest, _queryConfig.DefaultOrder, SortDirection.Desc, ct).ConfigureAwait(false);
                    if (result.IsSuccess)
                        return Results.Ok(result);

                    var error = ApiErrorResponseFactory.CreateForError(httpContext, result.Error);
                    return Results.Json(error, statusCode: error.Status);
                })
            .WithName($"QueryProject{Regex.Replace(typeof(TResponse).Name, "Res$", "")}")
            .WithTags(Regex.Replace(typeof(TResponse).Name, "Res$", ""))
            .Produces<ProjectedQueryRes<object?>>()
            .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest);

        ApplyAuthorization(projectedRouteBuilder, _queryConfig.Auth);
    }

    private void BuildQueryHistory()
    {
        if (_queryHistoryConfig == null)
            return;

        var routeBuilder = app.MapPost(
                $"{baseRoute}/QueryHistory", async (
                    [FromBody] HistoryQuery query,
                    [FromServices] IQueryHistoryService<TDbContext> service,
                    HttpContext httpContext,
                    CancellationToken ct = default) => {
                    var result = await service.QueryHistory<TDbEntity, TResponse>(
                            query, _queryHistoryConfig.DefaultOrder, _queryHistoryConfig.StartTimeSelector.Compile(), _queryHistoryConfig.EndTimeSelector.Compile(),
                            SortDirection.Desc, ct)
                        .ConfigureAwait(false);

                    if (result.IsSuccess)
                        return Results.Ok(result);

                    var error = ApiErrorResponseFactory.CreateForError(httpContext, result.Error);
                    return Results.Json(error, statusCode: error.Status);
                })
            .WithName($"Query{Regex.Replace(typeof(TResponse).Name, "Res$", "")}")
            .WithTags(Regex.Replace(typeof(TResponse).Name, "Res$", ""))
            .Produces<QueryHistoryResults<HistoryResult<TResponse>>>()
            .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest);

        ApplyAuthorization(routeBuilder, _queryHistoryConfig.Auth);
    }

    private void BuildExport()
    {
        if (_exportConfig == null)
            return;

        var serviceProviderIsService = app.Services.GetService<IServiceProviderIsService>();
        var exportServiceRegistered = serviceProviderIsService?.IsService(typeof(IExportService<TDbContext>)) == true;
        OperationHelpers.ThrowIf(
            !exportServiceRegistered,
            $"Export endpoint for '{typeof(TDbContext).Name}' requires '{nameof(IExportService<TDbContext>)}'. " +
            $"Register it with services.{nameof(ServiceCollectionExtensions.WithExportService)}<{typeof(TDbContext).Name}>().");

        var routeBuilder = app.MapPost(
                $"{baseRoute}/Export", async (
                    [FromBody] ExportRequest request,
                    [FromServices] IExportService<TDbContext> exportService,
                    HttpContext httpContext,
                    CancellationToken ct = default) => {
                    try {
                        var (stream, contentType, fileName) = await exportService.ExportAsync<TDbEntity, TResponse>(request, _exportConfig.DefaultOrder, SortDirection.Desc, ct)
                            .ConfigureAwait(false);

                        return Results.File(stream, contentType, fileName);
                    }
                    catch (ApiErrorException ex) {
                        return Results.Json(ApiErrorResponseFactory.CreateForError(httpContext, ex.ProblemDetails), statusCode: ex.ProblemDetails.Status);
                    }
                })
            .WithName($"Export{Regex.Replace(typeof(TResponse).Name, "Res$", "")}")
            .WithTags(Regex.Replace(typeof(TResponse).Name, "Res$", ""))
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        ApplyAuthorization(routeBuilder, _exportConfig.Auth);
    }

    private void BuildGet()
    {
        if (_getConfig == null)
            return;

        var routeBuilder = app.MapGet(
                $"{baseRoute}{ApiEndpointBuilderExtensions.GetDefaultEndpoint<TKey>()}", async (
                    TKey id,
                    [FromQuery] string[] include,
                    [FromServices] IQueryService<TDbContext> basicService,
                    HttpContext httpContext,
                    CancellationToken ct = default) => {
                    try {
                        var result = await basicService.Get<TDbEntity, TResponse>([id!], include, _getConfig.Before, _getConfig.After, ct).ConfigureAwait(false);
                        if (result is not null)
                            return Results.Ok(result);

                        var error = ApiErrorResponseFactory.CreateNotFound(httpContext, [id]);
                        return Results.Json(error, statusCode: error.Status);
                    }
                    catch (ApiErrorException ex) {
                        var error = ApiErrorResponseFactory.CreateForError(httpContext, ex.ProblemDetails);
                        return Results.Json(error, statusCode: error.Status);
                    }
                })
            .WithName($"Get{Regex.Replace(typeof(TResponse).Name, "Res$", "")}")
            .WithTags(Regex.Replace(typeof(TResponse).Name, "Res$", ""))
            .Produces<TResponse>()
            .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound);

        ApplyAuthorization(routeBuilder, _getConfig.Auth);
    }

    private void BuildCreate()
    {
        if (_createConfig == null)
            return;

        var routeBuilder = app.MapPost(
                $"{baseRoute}", async ([FromBody] TRequest request, [FromServices] ICreateService<TDbContext> basicService, HttpContext httpContext, CancellationToken ct = default)
                    => {
                    var result = await basicService.CreateAsync<TRequest, TDbEntity, TResponse>(request, _createConfig.Before, _createConfig.After, ct).ConfigureAwait(false);
                    if (result.IsSuccess)
                        return Results.Created($"{baseRoute}/{result.Data!.GetType().GetProperty("Id")?.GetValue(result.Data)}", result);

                    var error = ApiErrorResponseFactory.CreateForError(httpContext, result.Error);
                    return Results.Json(error, statusCode: error.Status);
                })
            .WithName($"Create{Regex.Replace(typeof(TResponse).Name, "Res$", "")}")
            .WithTags(Regex.Replace(typeof(TResponse).Name, "Res$", ""))
            .Produces<CreateResult<TResponse>>(StatusCodes.Status201Created)
            .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest);

        ApplyAuthorization(routeBuilder, _createConfig.Auth);
    }

    private void BuildCreateBulk()
    {
        if (_createBulkConfig == null)
            return;

        var routeBuilder = app.MapPost(
                $"{baseRoute}/Bulk", async ([FromBody] List<TRequest> requests, [FromServices] ICreateService<TDbContext> basicService, CancellationToken ct = default) => {
                    var result = await basicService.CreateBulkAsync<TRequest, TDbEntity, TResponse>(requests, _createBulkConfig.Before, _createBulkConfig.After, ct)
                        .ConfigureAwait(false);

                    return Results.Ok(result);
                })
            .WithName($"Create{Regex.Replace(typeof(TResponse).Name, "Res$", "")}Bulk")
            .WithTags(Regex.Replace(typeof(TResponse).Name, "Res$", ""))
            .Produces<CreateBulkResult<TResponse>>();

        ApplyAuthorization(routeBuilder, _createBulkConfig.Auth);
    }

    private void BuildUpdate()
    {
        if (_updateConfig == null)
            return;

        var routeBuilder = app.MapPost(
                $"{baseRoute}/Update", async (
                    [FromBody] UpdateRequest<TRequest> request,
                    [FromServices] IUpdateService<TDbContext> basicService,
                    HttpContext httpContext,
                    CancellationToken ct = default) => {
                    var result = await basicService.UpdateAsync<TRequest, TDbEntity, TResponse>(request, _updateConfig.Before, _updateConfig.After, ct).ConfigureAwait(false);
                    if (result.Result != UpdateResultEnum.Failed)
                        return Results.Ok(result);

                    var error = ApiErrorResponseFactory.CreateForError(httpContext, result.Error, request.Keys);
                    return Results.Json(error, statusCode: error.Status);
                })
            .WithName($"Update{Regex.Replace(typeof(TResponse).Name, "Res$", "")}")
            .WithTags(Regex.Replace(typeof(TResponse).Name, "Res$", ""))
            .Produces<UpdateResult<TResponse>>()
            .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound);

        ApplyAuthorization(routeBuilder, _updateConfig.Auth);
    }

    private void BuildUpdateBulk()
    {
        if (_updateBulkConfig == null)
            return;

        var routeBuilder = app.MapPost(
                $"{baseRoute}/Bulk/Update", async (
                    [FromBody] List<UpdateRequest<TRequest>> requests,
                    [FromServices] IUpdateService<TDbContext> basicService,
                    CancellationToken ct = default) => {
                    var result = await basicService.UpdateBulkAsync<TRequest, TDbEntity, TResponse>(requests, _updateBulkConfig.Before, _updateBulkConfig.After, ct)
                        .ConfigureAwait(false);

                    return Results.Ok(result);
                })
            .WithName($"Update{Regex.Replace(typeof(TResponse).Name, "Res$", "")}Bulk")
            .WithTags(Regex.Replace(typeof(TResponse).Name, "Res$", ""))
            .Produces<UpdateBulkResult<TResponse>>();

        ApplyAuthorization(routeBuilder, _updateBulkConfig.Auth);
    }

    private void BuildPatch()
    {
        if (_patchConfig == null)
            return;

        var routeBuilder = app.MapPatch(
                $"{baseRoute}", async (
                    [FromBody] PatchRequest request,
                    [FromServices] IPatchService<TDbContext> basicService,
                    HttpContext httpContext,
                    CancellationToken ct = default) => {
                    var fieldAuth = await PatchPropertyAuthorizationApplier
                        .ApplyAsync(_patchConfig.PropertyAuthorization, httpContext, typeof(TDbEntity), request, ct)
                        .ConfigureAwait(false);

                    if (!fieldAuth.Success) {
                        var err = ApiErrorResponseFactory.CreateForError(httpContext, fieldAuth.Error);
                        return Results.Json(err, statusCode: fieldAuth.Error!.Status);
                    }

                    request = fieldAuth.Request!;
                    var result = await basicService.PatchAsync<TDbEntity, TResponse>(request, _patchConfig.Before, _patchConfig.After, ct).ConfigureAwait(false);
                    var keys = request.Keys?.Cast<object?>().ToArray();
                    if (result.Result is PatchResultEnum.Updated or PatchResultEnum.NoChange)
                        return Results.Ok(result);

                    var error = ApiErrorResponseFactory.CreateForError(httpContext, result.Error, keys);
                    return Results.Json(error, statusCode: error.Status);
                })
            .WithName($"Patch{Regex.Replace(typeof(TResponse).Name, "Res$", "")}")
            .WithTags(Regex.Replace(typeof(TResponse).Name, "Res$", ""))
            .Produces<PatchResult<TResponse>>()
            .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound);

        ApplyAuthorization(routeBuilder, _patchConfig.Auth);
    }

    private void BuildPatchBulk()
    {
        if (_patchBulkConfig == null)
            return;

        var routeBuilder = app.MapPatch(
                $"{baseRoute}/Bulk", async (
                    [FromBody] List<PatchRequest> request,
                    [FromServices] IPatchService<TDbContext> basicService,
                    HttpContext httpContext,
                    CancellationToken ct = default) => {
                    if (_patchBulkConfig.PropertyAuthorization != null) {
                        var sanitized = new List<PatchRequest>(request.Count);
                        foreach (var patchRequest in request) {
                            var fieldAuth = await PatchPropertyAuthorizationApplier
                                .ApplyAsync(_patchBulkConfig.PropertyAuthorization, httpContext, typeof(TDbEntity), patchRequest, ct)
                                .ConfigureAwait(false);

                            if (!fieldAuth.Success) {
                                var err = ApiErrorResponseFactory.CreateForError(httpContext, fieldAuth.Error);
                                return Results.Json(err, statusCode: fieldAuth.Error!.Status);
                            }

                            sanitized.Add(fieldAuth.Request!);
                        }

                        request = sanitized;
                    }

                    var result = await basicService.PatchBulkAsync<TDbEntity, TResponse>(request, _patchBulkConfig.Before, _patchBulkConfig.After, ct).ConfigureAwait(false);
                    return Results.Ok(result);
                })
            .WithName($"Patch{Regex.Replace(typeof(TResponse).Name, "Res$", "")}Bulk")
            .WithTags(Regex.Replace(typeof(TResponse).Name, "Res$", ""))
            .Produces<PatchBulkResult<TResponse>>();

        ApplyAuthorization(routeBuilder, _patchBulkConfig.Auth);
    }

    private void BuildUpsert()
    {
        if (_upsertConfig == null)
            return;

        var routeBuilder = app.MapPost(
                $"{baseRoute}/Upsert", async (
                    [FromBody] UpsertRequest<TRequest> request,
                    [FromServices] IUpsertService<TDbContext> basicService,
                    HttpContext httpContext,
                    CancellationToken ct = default) => {
                    var result = await basicService.UpsertAsync<TRequest, TDbEntity, TResponse>(
                            request, _upsertConfig.Before, _upsertConfig.After, _upsertConfig.BeforeCreate, _upsertConfig.AfterCreate, _upsertConfig.BeforeUpdate,
                            _upsertConfig.AfterUpdate, ct)
                        .ConfigureAwait(false);

                    if (result.Result != UpsertResultEnum.Failed)
                        return Results.Ok(result);

                    var error = ApiErrorResponseFactory.CreateForError(httpContext, result.Error);
                    return Results.Json(error, statusCode: error.Status);
                })
            .WithName($"Upsert{Regex.Replace(typeof(TResponse).Name, "Res$", "")}")
            .WithTags(Regex.Replace(typeof(TResponse).Name, "Res$", ""))
            .Produces<UpsertResult<TResponse>>()
            .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<LyoProblemDetails>(StatusCodes.Status500InternalServerError);

        ApplyAuthorization(routeBuilder, _upsertConfig.Auth);
    }

    private void BuildUpsertBulk()
    {
        if (_upsertBulkConfig == null)
            return;

        var routeBuilder = app.MapPost(
                $"{baseRoute}/Bulk/Upsert", async (
                    [FromBody] List<UpsertRequest<TRequest>> requests,
                    [FromServices] IUpsertService<TDbContext> basicService,
                    CancellationToken ct = default) => {
                    var result = await basicService.UpsertBulkAsync<TRequest, TDbEntity, TResponse>(
                        requests, _upsertBulkConfig.Before, _upsertBulkConfig.After, _upsertBulkConfig.BeforeCreate, _upsertBulkConfig.AfterCreate, _upsertBulkConfig.BeforeUpdate,
                        _upsertBulkConfig.AfterUpdate, ct);

                    return Results.Ok(result);
                })
            .WithName($"Upsert{Regex.Replace(typeof(TResponse).Name, "Res$", "")}Bulk")
            .WithTags(Regex.Replace(typeof(TResponse).Name, "Res$", ""))
            .Produces<UpsertBulkResult<TResponse>>();

        ApplyAuthorization(routeBuilder, _upsertBulkConfig.Auth);
    }

    private void BuildDelete()
    {
        if (_deleteConfig == null)
            return;

        var routeBuilder1 = app.MapDelete(
                $"{baseRoute}{ApiEndpointBuilderExtensions.GetDefaultEndpoint<TKey>()}", async (
                    [FromRoute] TKey id,
                    [FromServices] IDeleteService<TDbContext> basicService,
                    HttpContext httpContext,
                    CancellationToken ct = default) => {
                    var result = await basicService.DeleteAsync<TDbEntity, TResponse>([id!], _deleteConfig.Before, _deleteConfig.After, _deleteConfig.Includes, ct)
                        .ConfigureAwait(false);

                    if (result.Error is null)
                        return Results.Ok(result);

                    var error = ApiErrorResponseFactory.CreateForError(httpContext, result.Error, [id]);
                    return Results.Json(error, statusCode: error.Status);
                })
            .WithName($"Delete{Regex.Replace(typeof(TResponse).Name, "Res$", "")}")
            .WithTags(Regex.Replace(typeof(TResponse).Name, "Res$", ""))
            .Produces<DeleteResult<TResponse>>()
            .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<LyoProblemDetails>(StatusCodes.Status500InternalServerError);

        ApplyAuthorization(routeBuilder1, _deleteConfig.Auth);
        var routeBuilder2 = app.MapDelete(
                $"{baseRoute}", async (
                    [FromBody] DeleteRequest request,
                    [FromServices] IDeleteService<TDbContext> basicService,
                    HttpContext httpContext,
                    CancellationToken ct = default) => {
                    var result = await basicService.DeleteAsync<TDbEntity, TResponse>(request, _deleteConfig.Before, _deleteConfig.After, _deleteConfig.Includes, ct)
                        .ConfigureAwait(false);

                    var keys = request.Keys?.Cast<object?>().ToArray();
                    if (result.Error is null)
                        return Results.Ok(result);

                    var error = ApiErrorResponseFactory.CreateForError(httpContext, result.Error, keys);
                    return Results.Json(error, statusCode: error.Status);
                })
            .WithName($"Delete{Regex.Replace(typeof(TResponse).Name, "Res$", "")}Request")
            .WithTags(Regex.Replace(typeof(TResponse).Name, "Res$", ""))
            .Produces<DeleteResult<TResponse>>()
            .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<LyoProblemDetails>(StatusCodes.Status500InternalServerError);

        ApplyAuthorization(routeBuilder2, _deleteConfig.Auth);
    }

    private void BuildDeleteBulk()
    {
        if (_deleteBulkConfig == null)
            return;

        var routeBuilder = app.MapDelete(
                $"{baseRoute}/Bulk", async ([FromBody] List<DeleteRequest> requests, [FromServices] IDeleteService<TDbContext> basicService, CancellationToken ct = default) => {
                    var result = await basicService.DeleteBulkAsync<TDbEntity, TResponse>(
                            requests, _deleteBulkConfig.Before, _deleteBulkConfig.After, _deleteBulkConfig.Includes, ct)
                        .ConfigureAwait(false);

                    return Results.Ok(result);
                })
            .WithName($"Delete{Regex.Replace(typeof(TResponse).Name, "Res$", "")}Bulk")
            .WithTags(Regex.Replace(typeof(TResponse).Name, "Res$", ""))
            .Produces<DeleteBulkResult<TResponse>>();

        ApplyAuthorization(routeBuilder, _deleteBulkConfig.Auth);
    }
}