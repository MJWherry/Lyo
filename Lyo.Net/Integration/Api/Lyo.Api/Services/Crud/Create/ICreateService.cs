using Lyo.Api.ApiEndpoint.Config;
using Lyo.Api.Models.Common.Response;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.Services.Crud.Create;

public interface ICreateService<TContext>
    where TContext : DbContext
{
    Task<CreateResult<TResult>> CreateAsync<TRequest, TDbModel, TResult>(
        TRequest request,
        Action<CreateContext<TRequest, TDbModel, TContext>>? before = null,
        Action<CreateContext<TRequest, TDbModel, TContext>>? after = null,
        CancellationToken ct = default)
        where TDbModel : class;

    Task<CreateBulkResult<TResult>> CreateBulkAsync<TRequest, TDbModel, TResult>(
        IEnumerable<TRequest> requests,
        Action<CreateContext<TRequest, TDbModel, TContext>>? before = null,
        Action<CreateContext<TRequest, TDbModel, TContext>>? after = null,
        CancellationToken ct = default)
        where TDbModel : class;
}