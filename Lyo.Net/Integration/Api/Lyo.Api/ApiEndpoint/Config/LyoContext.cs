using Lyo.Api.Models.Common.Request;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.ApiEndpoint.Config;

/// <summary>Base context for CRUD operations. Provides Entity, DbContext, and Services.</summary>
public abstract record LyoContextBase<TEntity, TDbContext>(TEntity Entity, TDbContext DbContext, IServiceProvider Services)
    where TDbContext : DbContext where TEntity : class;

/// <summary>Context for Create operations. Request is the create DTO before mapping to entity.</summary>
public sealed record CreateContext<TRequest, TEntity, TDbContext>(TRequest Request, TEntity Entity, TDbContext DbContext, IServiceProvider Services)
    : LyoContextBase<TEntity, TDbContext>(Entity, DbContext, Services)
    where TDbContext : DbContext where TEntity : class;

/// <summary>Context for Patch operations. Request is the PatchRequest.</summary>
public sealed record PatchContext<TEntity, TDbContext>(PatchRequest Request, TEntity Entity, TDbContext DbContext, IServiceProvider Services)
    : LyoContextBase<TEntity, TDbContext>(Entity, DbContext, Services)
    where TDbContext : DbContext where TEntity : class;

/// <summary>Context for Update operations. Request is the UpdateRequest.</summary>
public sealed record UpdateContext<TRequest, TEntity, TDbContext>(UpdateRequest<TRequest> Request, TEntity Entity, TDbContext DbContext, IServiceProvider Services)
    : LyoContextBase<TEntity, TDbContext>(Entity, DbContext, Services)
    where TDbContext : DbContext where TEntity : class;

/// <summary>Context for Get operations. Keys and Include are the lookup parameters.</summary>
public sealed record GetContext<TEntity, TDbContext>(object[] Keys, string[]? Include, TEntity Entity, TDbContext DbContext, IServiceProvider Services)
    : LyoContextBase<TEntity, TDbContext>(Entity, DbContext, Services)
    where TDbContext : DbContext where TEntity : class;

/// <summary>Context for Delete operations. Keys are always present; Request is non-null when deleting by DeleteRequest.</summary>
public sealed record DeleteContext<TEntity, TDbContext>(object[] Keys, DeleteRequest? Request, TEntity Entity, TDbContext DbContext, IServiceProvider Services)
    : LyoContextBase<TEntity, TDbContext>(Entity, DbContext, Services)
    where TDbContext : DbContext where TEntity : class;

/// <summary>Context for Upsert operations. Request is the UpsertRequest.</summary>
public sealed record UpsertContext<TRequest, TEntity, TDbContext>(UpsertRequest<TRequest> Request, TEntity Entity, TDbContext DbContext, IServiceProvider Services)
    : LyoContextBase<TEntity, TDbContext>(Entity, DbContext, Services)
    where TDbContext : DbContext where TEntity : class;