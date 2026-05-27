using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres.Database;

namespace Lyo.EntityReference.Postgres;

/// <summary>Shared helpers for Postgres association stores (soft-delete filtering, tenant filtering, interceptors).</summary>
/// <remarks>Tenant resolution lives in <see cref="TenancyResolver" /> and the per-store base; this class only owns query-time helpers.</remarks>
public static class EntityRefPostgresStoreHelpers
{
    /// <summary>Restricts a query to rows that are not soft-deleted (<see cref="EntityRefEntityBase.DeletedAt" /> is null).</summary>
    /// <typeparam name="T">Association entity deriving from <see cref="EntityRefEntityBase" />.</typeparam>
    /// <param name="query">Queryable source.</param>
    /// <returns>Filtered query.</returns>
    public static IQueryable<T> WhereActive<T>(this IQueryable<T> query)
        where T : EntityRefEntityBase
        => query.Where(e => e.DeletedAt == null);

    /// <summary>Restricts a query to a single tenant.</summary>
    /// <typeparam name="T">Association entity deriving from <see cref="EntityRefEntityBase" />.</typeparam>
    /// <param name="query">Queryable source.</param>
    /// <param name="tenantId">Tenant to match.</param>
    /// <returns>Filtered query.</returns>
    public static IQueryable<T> WhereTenant<T>(this IQueryable<T> query, Guid tenantId)
        where T : EntityRefEntityBase
        => query.Where(e => e.TenantId == tenantId);

    /// <summary>Runs interceptors sequentially.</summary>
    /// <param name="interceptors">Pipeline instances.</param>
    /// <param name="context">Phase and payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when all interceptors have run.</returns>
    public static async ValueTask RunInterceptorsAsync(IEnumerable<IEntityRefActionInterceptor> interceptors, EntityRefActionContext context, CancellationToken cancellationToken)
    {
        foreach (var interceptor in interceptors)
            await interceptor.InterceptAsync(context, cancellationToken).ConfigureAwait(false);
    }
}