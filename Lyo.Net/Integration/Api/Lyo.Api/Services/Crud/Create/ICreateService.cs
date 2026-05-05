using Lyo.Api.ApiEndpoint.Config;
using Lyo.Api.Models.Common.Response;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.Services.Crud.Create;

/// <summary>Persists new entities from request DTOs (single and bulk) with optional before/after hooks.</summary>
public interface ICreateService<TContext>
    where TContext : DbContext
{
    Task<CreateResult<TResult>> CreateAsync<TRequest, TDbModel, TResult>(
        TRequest request,
        Action<CreateContext<TRequest, TDbModel, TContext>>? before = null,
        Action<CreateContext<TRequest, TDbModel, TContext>>? after = null,
        Func<CreateContext<TRequest, TDbModel, TContext>, Task>? afterAsync = null,
        CancellationToken ct = default)
        where TDbModel : class;

    Task<CreateBulkResult<TResult>> CreateBulkAsync<TRequest, TDbModel, TResult>(
        IEnumerable<TRequest> requests,
        Action<CreateContext<TRequest, TDbModel, TContext>>? before = null,
        Action<CreateContext<TRequest, TDbModel, TContext>>? after = null,
        Func<CreateContext<TRequest, TDbModel, TContext>, Task>? afterAsync = null,
        CancellationToken ct = default)
        where TDbModel : class;
}