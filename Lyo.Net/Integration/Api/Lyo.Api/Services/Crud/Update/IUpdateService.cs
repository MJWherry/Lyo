using Lyo.Api.ApiEndpoint.Config;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.Services.Crud.Update;

/// <summary>Full replace updates from <c>UpdateRequest&lt;TRequest&gt;</c> bodies (single and bulk).</summary>
public interface IUpdateService<TContext>
    where TContext : DbContext
{
    Task<UpdateResult<TResult>> UpdateAsync<TRequest, TDbModel, TResult>(
        UpdateRequest<TRequest> request,
        Action<UpdateContext<TRequest, TDbModel, TContext>>? before = null,
        Action<UpdateContext<TRequest, TDbModel, TContext>>? after = null,
        CancellationToken ct = default)
        where TDbModel : class;

    Task<UpdateBulkResult<TResult>> UpdateBulkAsync<TRequest, TDbModel, TResult>(
        IEnumerable<UpdateRequest<TRequest>> requests,
        Action<UpdateContext<TRequest, TDbModel, TContext>>? before = null,
        Action<UpdateContext<TRequest, TDbModel, TContext>>? after = null,
        CancellationToken ct = default)
        where TDbModel : class;
}