using Lyo.Api.ApiEndpoint.Config;
using Lyo.Api.ApiEndpoint.Dynamic;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.ApiEndpoint;

/// <summary>Fluent builder for per-entity endpoint settings. Configure feature flags and operation-specific hooks.</summary>
public sealed class EntityEndpointConfigBuilder<TEntity, TContext>
    where TEntity : class where TContext : DbContext
{
    private CreateConfig<object, object, TContext>? _createConfig;
    private DeleteConfig<object, TContext>? _deleteConfig;
    private ExportConfig<object>? _exportConfig;
    private ApiFeatureSet _features;
    private PatchConfig<object, TContext>? _patchConfig;
    private UpdateConfig<object, object, TContext>? _updateConfig;
    private UpsertConfig<object, object, TContext>? _upsertConfig;

    internal EntityEndpointConfigBuilder(ApiFeatureSet defaultFeatures) => _features = defaultFeatures;

    /// <summary>Leave the Create endpoint off this entity.</summary>
    public EntityEndpointConfigBuilder<TEntity, TContext> ExcludeCreate()
    {
        _features = _features.Without(ApiFeature.Create);
        return this;
    }

    /// <summary>Leave the CreateBulk endpoint off this entity.</summary>
    public EntityEndpointConfigBuilder<TEntity, TContext> ExcludeCreateBulk()
    {
        _features = _features.Without(ApiFeature.CreateBulk);
        return this;
    }

    /// <summary>Leave the Update endpoint off this entity.</summary>
    public EntityEndpointConfigBuilder<TEntity, TContext> ExcludeUpdate()
    {
        _features = _features.Without(ApiFeature.Update, ApiFeature.UpdateBulk);
        return this;
    }

    /// <summary>Leave the UpdateBulk endpoint off this entity.</summary>
    public EntityEndpointConfigBuilder<TEntity, TContext> ExcludeUpdateBulk()
    {
        _features = _features.Without(ApiFeature.UpdateBulk);
        return this;
    }

    /// <summary>Leave the Patch endpoint off this entity.</summary>
    public EntityEndpointConfigBuilder<TEntity, TContext> ExcludePatch()
    {
        _features = _features.Without(ApiFeature.Patch, ApiFeature.PatchBulk);
        return this;
    }

    /// <summary>Leave the PatchBulk endpoint off this entity.</summary>
    public EntityEndpointConfigBuilder<TEntity, TContext> ExcludePatchBulk()
    {
        _features = _features.Without(ApiFeature.PatchBulk);
        return this;
    }

    /// <summary>Leave the Delete endpoint off this entity.</summary>
    public EntityEndpointConfigBuilder<TEntity, TContext> ExcludeDelete()
    {
        _features = _features.Without(ApiFeature.Delete, ApiFeature.DeleteBulk);
        return this;
    }

    /// <summary>Leave the DeleteBulk endpoint off this entity.</summary>
    public EntityEndpointConfigBuilder<TEntity, TContext> ExcludeDeleteBulk()
    {
        _features = _features.Without(ApiFeature.DeleteBulk);
        return this;
    }

    /// <summary>Leave the Upsert endpoint off this entity.</summary>
    public EntityEndpointConfigBuilder<TEntity, TContext> ExcludeUpsert()
    {
        _features = _features.Without(ApiFeature.Upsert, ApiFeature.UpsertBulk);
        return this;
    }

    /// <summary>Leave the UpsertBulk endpoint off this entity.</summary>
    public EntityEndpointConfigBuilder<TEntity, TContext> ExcludeUpsertBulk()
    {
        _features = _features.Without(ApiFeature.UpsertBulk);
        return this;
    }

    /// <summary>Leave the Export endpoint off this entity.</summary>
    public EntityEndpointConfigBuilder<TEntity, TContext> ExcludeExport()
    {
        _features = _features.WithoutName("Export");
        return this;
    }

    /// <summary>Leave the Query and QueryProject endpoints off this entity.</summary>
    public EntityEndpointConfigBuilder<TEntity, TContext> ExcludeQuery()
    {
        _features = _features.Without(ApiFeature.Query);
        return this;
    }

    /// <summary>Leave the Get endpoint off this entity.</summary>
    public EntityEndpointConfigBuilder<TEntity, TContext> ExcludeGet()
    {
        _features = _features.Without(ApiFeature.Get);
        return this;
    }

    /// <summary>Configure the Create endpoint for this entity.</summary>
    public EntityEndpointConfigBuilder<TEntity, TContext> ForCreate(Action<CreateConfigBuilder<TEntity, TContext>> configure)
    {
        var builder = new CreateConfigBuilder<TEntity, TContext>();
        configure(builder);
        _createConfig = builder.Build();
        return this;
    }

    /// <summary>Configure the Patch endpoint for this entity.</summary>
    public EntityEndpointConfigBuilder<TEntity, TContext> ForPatch(Action<PatchConfigBuilder<TEntity, TContext>> configure)
    {
        var builder = new PatchConfigBuilder<TEntity, TContext>();
        configure(builder);
        _patchConfig = builder.Build();
        return this;
    }

    /// <summary>Configure the Update endpoint for this entity.</summary>
    public EntityEndpointConfigBuilder<TEntity, TContext> ForUpdate(Action<UpdateConfigBuilder<TEntity, TContext>> configure)
    {
        var builder = new UpdateConfigBuilder<TEntity, TContext>();
        configure(builder);
        _updateConfig = builder.Build();
        return this;
    }

    /// <summary>Configure the Delete endpoint for this entity.</summary>
    public EntityEndpointConfigBuilder<TEntity, TContext> ForDelete(Action<DeleteConfigBuilder<TEntity, TContext>> configure)
    {
        var builder = new DeleteConfigBuilder<TEntity, TContext>();
        configure(builder);
        _deleteConfig = builder.Build();
        return this;
    }

    /// <summary>Configure the Upsert endpoint for this entity.</summary>
    public EntityEndpointConfigBuilder<TEntity, TContext> ForUpsert(Action<UpsertConfigBuilder<TEntity, TContext>> configure)
    {
        var builder = new UpsertConfigBuilder<TEntity, TContext>();
        configure(builder);
        _upsertConfig = builder.Build();
        return this;
    }

    /// <summary>Configure the Export endpoint for this entity.</summary>
    public EntityEndpointConfigBuilder<TEntity, TContext> ForExport(Action<ExportConfigBuilder<TEntity>> configure)
    {
        var builder = new ExportConfigBuilder<TEntity>();
        configure(builder);
        _exportConfig = builder.Build();
        return this;
    }

    /// <summary>Sets the BeforeCreate hook (runs before the entity is inserted).</summary>
    public EntityEndpointConfigBuilder<TEntity, TContext> BeforeCreate(Action<CreateContext<object, TEntity, TContext>> before)
    {
        _createConfig = new() {
            Before = ctx => before(new(ctx.Request, (TEntity)ctx.Entity, ctx.DbContext, ctx.Services)), After = _createConfig?.After, Auth = _createConfig?.Auth
        };

        return this;
    }

    internal EntityEndpointConfig<TContext> Build() => new(_features, _createConfig, _patchConfig, _updateConfig, _deleteConfig, _upsertConfig, _exportConfig);
}

/// <summary>Fluent builder for Create endpoint settings.</summary>
public sealed class CreateConfigBuilder<TEntity, TContext>
    where TEntity : class where TContext : DbContext
{
    private Action<CreateContext<object, TEntity, TContext>>? _after;
    private EndpointAuth? _auth;
    private Action<CreateContext<object, TEntity, TContext>>? _before;

    public CreateConfigBuilder<TEntity, TContext> Before(Action<CreateContext<object, TEntity, TContext>> before)
    {
        _before = before;
        return this;
    }

    public CreateConfigBuilder<TEntity, TContext> After(Action<CreateContext<object, TEntity, TContext>> after)
    {
        _after = after;
        return this;
    }

    public CreateConfigBuilder<TEntity, TContext> Auth(EndpointAuth auth)
    {
        _auth = auth;
        return this;
    }

    internal CreateConfig<object, object, TContext> Build()
        => new() {
            Before = _before == null ? null : ctx => _before(new(ctx.Request, (TEntity)ctx.Entity, ctx.DbContext, ctx.Services)),
            After = _after == null ? null : ctx => _after(new(ctx.Request, (TEntity)ctx.Entity, ctx.DbContext, ctx.Services)),
            Auth = _auth
        };
}

/// <summary>Fluent builder for Patch endpoint settings.</summary>
public sealed class PatchConfigBuilder<TEntity, TContext>
    where TEntity : class where TContext : DbContext
{
    private Action<PatchContext<TEntity, TContext>>? _after;
    private EndpointAuth? _auth;
    private Action<PatchContext<TEntity, TContext>>? _before;
    private PatchPropertyAuthorization? _propertyAuthorization;

    public PatchConfigBuilder<TEntity, TContext> Before(Action<PatchContext<TEntity, TContext>> before)
    {
        _before = before;
        return this;
    }

    public PatchConfigBuilder<TEntity, TContext> After(Action<PatchContext<TEntity, TContext>> after)
    {
        _after = after;
        return this;
    }

    public PatchConfigBuilder<TEntity, TContext> Auth(EndpointAuth auth)
    {
        _auth = auth;
        return this;
    }

    /// <summary>Restricts patch property keys by policy map or by custom logic.</summary>
    public PatchConfigBuilder<TEntity, TContext> PropertyAuthorization(PatchPropertyAuthorization propertyAuthorization)
    {
        _propertyAuthorization = propertyAuthorization;
        return this;
    }

    /// <summary>Builds <see cref="PatchPropertyAuthorization" /> from a policy-to-property map.</summary>
    public PatchConfigBuilder<TEntity, TContext> PropertyAuthorization(Action<PatchPropertyAuthorizationBuilder> configure)
    {
        _propertyAuthorization = PatchPropertyAuthorization.ForPolicies(configure);
        return this;
    }

    internal PatchConfig<object, TContext> Build()
        => new() {
            Before = _before == null ? null : ctx => _before(new(ctx.Request, (TEntity)ctx.Entity, ctx.DbContext, ctx.Services)),
            After = _after == null ? null : ctx => _after(new(ctx.Request, (TEntity)ctx.Entity, ctx.DbContext, ctx.Services)),
            Auth = _auth,
            PropertyAuthorization = _propertyAuthorization
        };
}

/// <summary>Fluent builder for Update endpoint settings.</summary>
public sealed class UpdateConfigBuilder<TEntity, TContext>
    where TEntity : class where TContext : DbContext
{
    private Action<UpdateContext<object, TEntity, TContext>>? _after;
    private EndpointAuth? _auth;
    private Action<UpdateContext<object, TEntity, TContext>>? _before;

    public UpdateConfigBuilder<TEntity, TContext> Before(Action<UpdateContext<object, TEntity, TContext>> before)
    {
        _before = before;
        return this;
    }

    public UpdateConfigBuilder<TEntity, TContext> After(Action<UpdateContext<object, TEntity, TContext>> after)
    {
        _after = after;
        return this;
    }

    public UpdateConfigBuilder<TEntity, TContext> Auth(EndpointAuth auth)
    {
        _auth = auth;
        return this;
    }

    internal UpdateConfig<object, object, TContext> Build()
        => new() {
            Before = _before == null ? null : ctx => _before(new(ctx.Request, (TEntity)ctx.Entity, ctx.DbContext, ctx.Services)),
            After = _after == null ? null : ctx => _after(new(ctx.Request, (TEntity)ctx.Entity, ctx.DbContext, ctx.Services)),
            Auth = _auth
        };
}

/// <summary>Fluent builder for Delete endpoint settings.</summary>
public sealed class DeleteConfigBuilder<TEntity, TContext>
    where TEntity : class where TContext : DbContext
{
    private Action<DeleteContext<TEntity, TContext>>? _after;
    private EndpointAuth? _auth;
    private Action<DeleteContext<TEntity, TContext>>? _before;
    private string[]? _includes;

    public DeleteConfigBuilder<TEntity, TContext> Before(Action<DeleteContext<TEntity, TContext>> before)
    {
        _before = before;
        return this;
    }

    public DeleteConfigBuilder<TEntity, TContext> After(Action<DeleteContext<TEntity, TContext>> after)
    {
        _after = after;
        return this;
    }

    public DeleteConfigBuilder<TEntity, TContext> Includes(params string[] includes)
    {
        _includes = includes;
        return this;
    }

    public DeleteConfigBuilder<TEntity, TContext> Auth(EndpointAuth auth)
    {
        _auth = auth;
        return this;
    }

    internal DeleteConfig<object, TContext> Build()
        => new() {
            Before = _before == null ? null : ctx => _before(new(ctx.Keys, ctx.Request, (TEntity)ctx.Entity, ctx.DbContext, ctx.Services)),
            After = _after == null ? null : ctx => _after(new(ctx.Keys, ctx.Request, (TEntity)ctx.Entity, ctx.DbContext, ctx.Services)),
            Includes = _includes,
            Auth = _auth
        };
}

/// <summary>Fluent builder for Upsert endpoint settings.</summary>
public sealed class UpsertConfigBuilder<TEntity, TContext>
    where TEntity : class where TContext : DbContext
{
    private Action<UpsertContext<object, TEntity, TContext>>? _after;
    private Action<UpsertContext<object, TEntity, TContext>>? _afterCreate;
    private Action<UpsertContext<object, TEntity, TContext>>? _afterUpdate;
    private EndpointAuth? _auth;
    private Action<UpsertContext<object, TEntity, TContext>>? _before;
    private Action<UpsertContext<object, TEntity, TContext>>? _beforeCreate;
    private Action<UpsertContext<object, TEntity, TContext>>? _beforeUpdate;

    public UpsertConfigBuilder<TEntity, TContext> Before(Action<UpsertContext<object, TEntity, TContext>> before)
    {
        _before = before;
        return this;
    }

    public UpsertConfigBuilder<TEntity, TContext> BeforeCreate(Action<UpsertContext<object, TEntity, TContext>> before)
    {
        _beforeCreate = before;
        return this;
    }

    public UpsertConfigBuilder<TEntity, TContext> BeforeUpdate(Action<UpsertContext<object, TEntity, TContext>> before)
    {
        _beforeUpdate = before;
        return this;
    }

    public UpsertConfigBuilder<TEntity, TContext> After(Action<UpsertContext<object, TEntity, TContext>> after)
    {
        _after = after;
        return this;
    }

    public UpsertConfigBuilder<TEntity, TContext> AfterCreate(Action<UpsertContext<object, TEntity, TContext>> after)
    {
        _afterCreate = after;
        return this;
    }

    public UpsertConfigBuilder<TEntity, TContext> AfterUpdate(Action<UpsertContext<object, TEntity, TContext>> after)
    {
        _afterUpdate = after;
        return this;
    }

    public UpsertConfigBuilder<TEntity, TContext> Auth(EndpointAuth auth)
    {
        _auth = auth;
        return this;
    }

    internal UpsertConfig<object, object, TContext> Build()
        => new() {
            Before = _before == null ? null : ctx => _before(new(ctx.Request, (TEntity)ctx.Entity, ctx.DbContext, ctx.Services)),
            BeforeCreate = _beforeCreate == null ? null : ctx => _beforeCreate(new(ctx.Request, (TEntity)ctx.Entity, ctx.DbContext, ctx.Services)),
            BeforeUpdate = _beforeUpdate == null ? null : ctx => _beforeUpdate(new(ctx.Request, (TEntity)ctx.Entity, ctx.DbContext, ctx.Services)),
            After = _after == null ? null : ctx => _after(new(ctx.Request, (TEntity)ctx.Entity, ctx.DbContext, ctx.Services)),
            AfterCreate = _afterCreate == null ? null : ctx => _afterCreate(new(ctx.Request, (TEntity)ctx.Entity, ctx.DbContext, ctx.Services)),
            AfterUpdate = _afterUpdate == null ? null : ctx => _afterUpdate(new(ctx.Request, (TEntity)ctx.Entity, ctx.DbContext, ctx.Services)),
            Auth = _auth
        };
}

/// <summary>Fluent builder for Export endpoint settings.</summary>
public sealed class ExportConfigBuilder<TEntity>
    where TEntity : class
{
    private EndpointAuth? _auth;

    public ExportConfigBuilder<TEntity> Auth(EndpointAuth auth)
    {
        _auth = auth;
        return this;
    }

    internal ExportConfig<object> Build()
        => new() {
            GroupName = typeof(TEntity).Name,
            DefaultOrder = null!, // Dynamic uses default from metadata
            Auth = _auth
        };
}