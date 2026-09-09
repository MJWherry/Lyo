using Lyo.EntityReference.Postgres.Database;

namespace Lyo.EntityReference.Postgres;

/// <summary>Shared helpers for optional-actor relation rows (audit and change-tracker).</summary>
public static class EntityRelationOptionalActorHelpers
{
    extension<T>(IQueryable<T> query)
        where T : EntityRelationOptionalActorBase
    {
        /// <summary>Keeps rows whose tenant exactly matches <paramref name="tenantId" /> (including null for system rows).</summary>
        public IQueryable<T> WhereTenant(Guid? tenantId) => query.Where(e => e.TenantId == tenantId);

        /// <summary>Keeps rows belonging to <paramref name="tenantId" /> or system rows (<c>tenant_id IS NULL</c>).</summary>
        public IQueryable<T> WhereTenantOrSystem(Guid tenantId) => query.Where(e => e.TenantId == tenantId || e.TenantId == null);
    }
}