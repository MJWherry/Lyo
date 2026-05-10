using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres.Database;

namespace Lyo.EntityReference.Postgres;

/// <summary>Shared helpers for Postgres association stores (tenant resolution, soft-delete filtering, interceptors).</summary>
public static class EntityRefPostgresStoreHelpers
{
    /// <summary>Resolves nullable caller tenant to a concrete id using <see cref="EntityRefOptions.DefaultTenantId"/>.</summary>
    /// <param name="tenantId">Explicit tenant when provided.</param>
    /// <param name="options">Host options supplying the fallback tenant.</param>
    /// <returns><paramref name="tenantId"/> when non-null and non-empty; otherwise <see cref="EntityRefOptions.DefaultTenantId"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    public static Guid ResolveTenantId(Guid? tenantId, EntityRefOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (tenantId is { } t && t != Guid.Empty)
            return t;

        return options.DefaultTenantId;
    }

    /// <summary>Restricts a query to rows that are not soft-deleted (<see cref="EntityRefEntityBase.DeletedAt"/> is null).</summary>
    /// <typeparam name="T">Association entity deriving from <see cref="EntityRefEntityBase"/>.</typeparam>
    /// <param name="query">Queryable source.</param>
    /// <returns>Filtered query.</returns>
    public static IQueryable<T> WhereActive<T>(this IQueryable<T> query)
        where T : EntityRefEntityBase
        => query.Where(e => e.DeletedAt == null);

    /// <summary>Restricts a query to a single tenant.</summary>
    /// <typeparam name="T">Association entity deriving from <see cref="EntityRefEntityBase"/>.</typeparam>
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
    public static async ValueTask RunInterceptorsAsync(
        IEnumerable<IEntityRefActionInterceptor> interceptors,
        EntityRefActionContext context,
        CancellationToken cancellationToken)
    {
        foreach (var interceptor in interceptors)
            await interceptor.InterceptAsync(context, cancellationToken).ConfigureAwait(false);
    }
}
