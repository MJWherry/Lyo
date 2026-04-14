using Lyo.Api.ApiEndpoint;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.ApiEndpoint.Config;

/// <summary>Fluent builder for <c>WithCrud</c> on the API endpoint builder. Call <see cref="WithFlags"/> first.</summary>
public sealed class CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest>
    where TDbContext : DbContext where TDbEntity : class
{
    private ApiFeatureFlag _flags;
    private bool _flagsSet;

    private Action<GetContext<TDbEntity, TDbContext>>? _afterGet;
    private Action<CreateContext<TRequest, TDbEntity, TDbContext>>? _afterCreate;
    private Action<UpdateContext<TRequest, TDbEntity, TDbContext>>? _afterUpdate;
    private Action<PatchContext<TDbEntity, TDbContext>>? _afterPatch;
    private Action<UpsertContext<TRequest, TDbEntity, TDbContext>>? _afterUpsert;
    private Action<DeleteContext<TDbEntity, TDbContext>>? _afterDelete;
    private Action<GetContext<TDbEntity, TDbContext>>? _beforeGet;
    private Action<CreateContext<TRequest, TDbEntity, TDbContext>>? _beforeCreate;
    private Action<UpdateContext<TRequest, TDbEntity, TDbContext>>? _beforeUpdate;
    private Action<PatchContext<TDbEntity, TDbContext>>? _beforePatch;
    private Action<UpsertContext<TRequest, TDbEntity, TDbContext>>? _beforeUpsert;
    private Action<DeleteContext<TDbEntity, TDbContext>>? _beforeDelete;

    private string[]? _deleteIncludes;
    private MetadataConfiguration<TDbContext, TDbEntity>? _metadata;

    private EndpointAuth? _createAuth;
    private EndpointAuth? _createBulkAuth;
    private EndpointAuth? _deleteAuth;
    private EndpointAuth? _deleteBulkAuth;
    private EndpointAuth? _exportAuth;
    private EndpointAuth? _getAuth;
    private EndpointAuth? _metadataAuth;
    private EndpointAuth? _patchAuth;
    private EndpointAuth? _patchBulkAuth;
    private PatchPropertyAuthorization? _patchPropertyAuthorization;
    private EndpointAuth? _queryAuth;
    private EndpointAuth? _queryHistoryAuth;
    private EndpointAuth? _updateAuth;
    private EndpointAuth? _updateBulkAuth;
    private EndpointAuth? _upsertAuth;
    private EndpointAuth? _upsertBulkAuth;

    /// <summary>Which endpoints and behaviors to register (required).</summary>
    public CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest> WithFlags(ApiFeatureFlag features)
    {
        _flags = features;
        _flagsSet = true;
        return this;
    }

    public CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest> BeforeGet(Action<GetContext<TDbEntity, TDbContext>> before)
    {
        _beforeGet = before;
        return this;
    }

    public CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest> AfterGet(Action<GetContext<TDbEntity, TDbContext>> after)
    {
        _afterGet = after;
        return this;
    }

    public CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest> BeforeCreate(Action<CreateContext<TRequest, TDbEntity, TDbContext>> before)
    {
        _beforeCreate = before;
        return this;
    }

    public CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest> AfterCreate(Action<CreateContext<TRequest, TDbEntity, TDbContext>> after)
    {
        _afterCreate = after;
        return this;
    }

    public CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest> BeforeUpdate(Action<UpdateContext<TRequest, TDbEntity, TDbContext>> before)
    {
        _beforeUpdate = before;
        return this;
    }

    public CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest> AfterUpdate(Action<UpdateContext<TRequest, TDbEntity, TDbContext>> after)
    {
        _afterUpdate = after;
        return this;
    }

    public CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest> BeforePatch(Action<PatchContext<TDbEntity, TDbContext>> before)
    {
        _beforePatch = before;
        return this;
    }

    public CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest> AfterPatch(Action<PatchContext<TDbEntity, TDbContext>> after)
    {
        _afterPatch = after;
        return this;
    }

    public CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest> BeforeUpsert(Action<UpsertContext<TRequest, TDbEntity, TDbContext>> before)
    {
        _beforeUpsert = before;
        return this;
    }

    public CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest> AfterUpsert(Action<UpsertContext<TRequest, TDbEntity, TDbContext>> after)
    {
        _afterUpsert = after;
        return this;
    }

    public CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest> BeforeDelete(Action<DeleteContext<TDbEntity, TDbContext>> before)
    {
        _beforeDelete = before;
        return this;
    }

    public CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest> AfterDelete(Action<DeleteContext<TDbEntity, TDbContext>> after)
    {
        _afterDelete = after;
        return this;
    }

    public CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest> DeleteIncludes(params string[] includes)
    {
        _deleteIncludes = includes;
        return this;
    }

    public CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest> Metadata(MetadataConfiguration<TDbContext, TDbEntity> metadata)
    {
        _metadata = metadata;
        return this;
    }

    public CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest> QueryAuth(EndpointAuth auth)
    {
        _queryAuth = auth;
        return this;
    }

    public CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest> QueryHistoryAuth(EndpointAuth auth)
    {
        _queryHistoryAuth = auth;
        return this;
    }

    public CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest> GetAuth(EndpointAuth auth)
    {
        _getAuth = auth;
        return this;
    }

    public CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest> CreateAuth(EndpointAuth auth)
    {
        _createAuth = auth;
        return this;
    }

    public CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest> CreateBulkAuth(EndpointAuth auth)
    {
        _createBulkAuth = auth;
        return this;
    }

    public CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest> UpdateAuth(EndpointAuth auth)
    {
        _updateAuth = auth;
        return this;
    }

    public CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest> UpdateBulkAuth(EndpointAuth auth)
    {
        _updateBulkAuth = auth;
        return this;
    }

    public CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest> PatchAuth(EndpointAuth auth)
    {
        _patchAuth = auth;
        return this;
    }

    public CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest> PatchBulkAuth(EndpointAuth auth)
    {
        _patchBulkAuth = auth;
        return this;
    }

    public CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest> PatchPropertyAuthorization(PatchPropertyAuthorization authorization)
    {
        _patchPropertyAuthorization = authorization;
        return this;
    }

    /// <summary>Policy-based allowed patch properties (same as <see cref="PatchPropertyAuthorization.ForPolicies"/>). For a custom delegate, pass a built <see cref="PatchPropertyAuthorization"/> to the other overload.</summary>
    public CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest> PatchPropertyAuthorization(Action<PatchPropertyAuthorizationBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _patchPropertyAuthorization = global::Lyo.Api.ApiEndpoint.Config.PatchPropertyAuthorization.ForPolicies(configure);
        return this;
    }

    public CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest> UpsertAuth(EndpointAuth auth)
    {
        _upsertAuth = auth;
        return this;
    }

    public CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest> UpsertBulkAuth(EndpointAuth auth)
    {
        _upsertBulkAuth = auth;
        return this;
    }

    public CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest> DeleteAuth(EndpointAuth auth)
    {
        _deleteAuth = auth;
        return this;
    }

    public CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest> DeleteBulkAuth(EndpointAuth auth)
    {
        _deleteBulkAuth = auth;
        return this;
    }

    public CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest> ExportAuth(EndpointAuth auth)
    {
        _exportAuth = auth;
        return this;
    }

    public CrudConfigurationBuilder<TDbContext, TDbEntity, TRequest> MetadataAuth(EndpointAuth auth)
    {
        _metadataAuth = auth;
        return this;
    }

    internal (ApiFeatureFlag Features, CrudConfiguration<TDbContext, TDbEntity, TRequest> Config) Build()
    {
        if (!_flagsSet)
            throw new InvalidOperationException("Call WithFlags(...) to specify ApiFeatureFlag values for WithCrud (e.g. ApiFeatureFlag.All).");

        var config = new CrudConfiguration<TDbContext, TDbEntity, TRequest> {
            AfterCreate = _afterCreate,
            AfterDelete = _afterDelete,
            AfterGet = _afterGet,
            AfterPatch = _afterPatch,
            AfterUpdate = _afterUpdate,
            AfterUpsert = _afterUpsert,
            BeforeCreate = _beforeCreate,
            BeforeDelete = _beforeDelete,
            BeforeGet = _beforeGet,
            BeforePatch = _beforePatch,
            BeforeUpdate = _beforeUpdate,
            BeforeUpsert = _beforeUpsert,
            CreateAuth = _createAuth,
            CreateBulkAuth = _createBulkAuth,
            DeleteAuth = _deleteAuth,
            DeleteBulkAuth = _deleteBulkAuth,
            DeleteIncludes = _deleteIncludes,
            ExportAuth = _exportAuth,
            GetAuth = _getAuth,
            Metadata = _metadata ?? new MetadataConfiguration<TDbContext, TDbEntity>(),
            MetadataAuth = _metadataAuth,
            PatchAuth = _patchAuth,
            PatchBulkAuth = _patchBulkAuth,
            PatchPropertyAuthorization = _patchPropertyAuthorization,
            QueryAuth = _queryAuth,
            QueryHistoryAuth = _queryHistoryAuth,
            UpdateAuth = _updateAuth,
            UpdateBulkAuth = _updateBulkAuth,
            UpsertAuth = _upsertAuth,
            UpsertBulkAuth = _upsertBulkAuth
        };

        return (_flags, config);
    }
}
