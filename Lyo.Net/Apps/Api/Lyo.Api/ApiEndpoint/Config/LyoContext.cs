using Lyo.Api.Models.Common.Request;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.ApiEndpoint.Config;

/// <summary>Base context for CRUD operations. Exposes Entity, DbContext, and Services.</summary>
public abstract record LyoContextBase<TEntity, TDbContext>(TEntity Entity, TDbContext DbContext, IServiceProvider Services)
    where TDbContext : DbContext where TEntity : class;

/// <summary>Context for Create operations. Request is the create DTO before it is mapped to an entity.</summary>
public sealed record CreateContext<TRequest, TEntity, TDbContext>(TRequest Request, TEntity Entity, TDbContext DbContext, IServiceProvider Services)
    : LyoContextBase<TEntity, TDbContext>(Entity, DbContext, Services)
    where TDbContext : DbContext where TEntity : class;

/// <summary>Context for Patch operations. Request is the PatchRequest payload.</summary>
public sealed record PatchContext<TEntity, TDbContext>(PatchRequest Request, TEntity Entity, TDbContext DbContext, IServiceProvider Services)
    : LyoContextBase<TEntity, TDbContext>(Entity, DbContext, Services)
    where TDbContext : DbContext where TEntity : class;

/// <summary>Context for Update operations. Request is the UpdateRequest payload.</summary>
public sealed record UpdateContext<TRequest, TEntity, TDbContext>(UpdateRequest<TRequest> Request, TEntity Entity, TDbContext DbContext, IServiceProvider Services)
    : LyoContextBase<TEntity, TDbContext>(Entity, DbContext, Services)
    where TDbContext : DbContext where TEntity : class;

/// <summary>Context for Get operations. Keys and Include are the lookup arguments.</summary>
public sealed record GetContext<TEntity, TDbContext>(object[] Keys, string[]? Include, TEntity Entity, TDbContext DbContext, IServiceProvider Services)
    : LyoContextBase<TEntity, TDbContext>(Entity, DbContext, Services)
    where TDbContext : DbContext where TEntity : class;

/// <summary>Context for Delete operations. Keys are always present. Request is non-null when deleting by DeleteRequest.</summary>
public sealed record DeleteContext<TEntity, TDbContext>(object[] Keys, DeleteRequest? Request, TEntity Entity, TDbContext DbContext, IServiceProvider Services)
    : LyoContextBase<TEntity, TDbContext>(Entity, DbContext, Services)
    where TDbContext : DbContext where TEntity : class;

/// <summary>Context for Upsert operations. Request is the UpsertRequest payload.</summary>
public sealed record UpsertContext<TRequest, TEntity, TDbContext>(UpsertRequest<TRequest> Request, TEntity Entity, TDbContext DbContext, IServiceProvider Services)
    : LyoContextBase<TEntity, TDbContext>(Entity, DbContext, Services)
    where TDbContext : DbContext where TEntity : class;