using Lyo.Api.ApiEndpoint.Config;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.Services.Crud.Update;

public interface IUpsertService<TContext>
    where TContext : DbContext
{
    Task<UpsertResult<TResult>> UpsertAsync<TRequest, TDbModel, TResult>(
        UpsertRequest<TRequest> request,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? before = null,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? after = null,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? beforeCreate = null,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? afterCreate = null,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? beforeUpdate = null,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? afterUpdate = null,
        CancellationToken ct = default)
        where TDbModel : class;

    Task<UpsertBulkResult<TResult>> UpsertBulkAsync<TRequest, TDbModel, TResult>(
        IEnumerable<UpsertRequest<TRequest>> requests,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? before = null,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? after = null,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? beforeCreate = null,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? afterCreate = null,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? beforeUpdate = null,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? afterUpdate = null,
        CancellationToken ct = default)
        where TDbModel : class;
}