using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.ApiEndpoint.Config;

/// <summary>Upsert endpoint config. Use UpsertConfig&lt;object, object, DbContext&gt; for dynamic endpoints.</summary>
public sealed record UpsertConfig<TRequest, TEntity, TDbContext>
    where TDbContext : DbContext where TEntity : class
{
    public Action<UpsertContext<TRequest, TEntity, TDbContext>>? Before { get; init; }

    public Action<UpsertContext<TRequest, TEntity, TDbContext>>? BeforeCreate { get; init; }

    public Action<UpsertContext<TRequest, TEntity, TDbContext>>? BeforeUpdate { get; init; }

    public Action<UpsertContext<TRequest, TEntity, TDbContext>>? After { get; init; }

    public Action<UpsertContext<TRequest, TEntity, TDbContext>>? AfterCreate { get; init; }

    public Action<UpsertContext<TRequest, TEntity, TDbContext>>? AfterUpdate { get; init; }

    public string Endpoint { get; set; } = "/Upsert";

    public EndpointAuth? Auth { get; init; }
}