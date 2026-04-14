using Lyo.Api.ApiEndpoint.Config;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.Services.Crud.Update;

public interface IPatchService<TContext>
    where TContext : DbContext
{
    Task<PatchResult<TResult>> PatchAsync<TDbModel, TResult>(
        PatchRequest request,
        Action<PatchContext<TDbModel, TContext>>? before = null,
        Action<PatchContext<TDbModel, TContext>>? after = null,
        CancellationToken ct = default)
        where TDbModel : class;

    Task<PatchBulkResult<TResult>> PatchBulkAsync<TDbModel, TResult>(
        IEnumerable<PatchRequest> requests,
        Action<PatchContext<TDbModel, TContext>>? before = null,
        Action<PatchContext<TDbModel, TContext>>? after = null,
        CancellationToken ct = default)
        where TDbModel : class;
}