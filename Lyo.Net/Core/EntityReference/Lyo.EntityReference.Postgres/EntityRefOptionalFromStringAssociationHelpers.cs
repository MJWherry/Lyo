using Lyo.EntityReference.Postgres.Database;

namespace Lyo.EntityReference.Postgres;

/// <summary>Shared query helpers for Postgres association stores backed by <see cref="EntityRefOptionalFromStringAssociationBase" />.</summary>
public static class EntityRefOptionalFromStringAssociationHelpers
{
    /// <summary>Restricts a query to rows whose <see cref="EntityRefOptionalFromStringAssociationBase.TenantId" /> exactly matches <paramref name="tenantId" />.</summary>
    /// <remarks>Pass <see langword="null" /> to match only system / untenanted rows. Use <see cref="WhereTenantOrSystem{T}" /> to include both.</remarks>
    /// <typeparam name="T">Entity deriving from <see cref="EntityRefOptionalFromStringAssociationBase" />.</typeparam>
    /// <param name="query">Queryable source.</param>
    /// <param name="tenantId">Tenant to match (or <see langword="null" /> for untenanted rows).</param>
    /// <returns>Filtered query.</returns>
    public static IQueryable<T> WhereTenant<T>(this IQueryable<T> query, Guid? tenantId)
        where T : EntityRefOptionalFromStringAssociationBase
        => query.Where(e => e.TenantId == tenantId);

    /// <summary>Restricts a query to rows belonging to <paramref name="tenantId" /> OR system rows (<c>tenant_id IS NULL</c>).</summary>
    /// <typeparam name="T">Entity deriving from <see cref="EntityRefOptionalFromStringAssociationBase" />.</typeparam>
    /// <param name="query">Queryable source.</param>
    /// <param name="tenantId">Tenant to match.</param>
    /// <returns>Filtered query.</returns>
    public static IQueryable<T> WhereTenantOrSystem<T>(this IQueryable<T> query, Guid tenantId)
        where T : EntityRefOptionalFromStringAssociationBase
        => query.Where(e => e.TenantId == tenantId || e.TenantId == null);
}