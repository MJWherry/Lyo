using Lyo.Api.ApiEndpoint.Config;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.Services.Crud.Delete;

public interface IDeleteService<TContext>
    where TContext : DbContext
{
    Task<DeleteResult<TResult>> DeleteAsync<TDbModel, TResult>(
        object[] keys,
        Action<DeleteContext<TDbModel, TContext>>? before = null,
        Action<DeleteContext<TDbModel, TContext>>? after = null,
        IEnumerable<string>? includes = null,
        CancellationToken ct = default)
        where TDbModel : class;

    Task<DeleteResult<TResult>> DeleteAsync<TDbModel, TResult>(
        DeleteRequest request,
        Action<DeleteContext<TDbModel, TContext>>? before = null,
        Action<DeleteContext<TDbModel, TContext>>? after = null,
        IEnumerable<string>? includes = null,
        CancellationToken ct = default)
        where TDbModel : class;

    Task<DeleteBulkResult<TResult>> DeleteBulkAsync<TDbModel, TResult>(
        IEnumerable<DeleteRequest> requests,
        Action<DeleteContext<TDbModel, TContext>>? before = null,
        Action<DeleteContext<TDbModel, TContext>>? after = null,
        IEnumerable<string>? includes = null,
        CancellationToken ct = default)
        where TDbModel : class;
}