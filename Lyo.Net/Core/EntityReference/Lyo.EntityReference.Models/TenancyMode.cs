namespace Lyo.EntityReference.Models;

/// <summary>Per-feature tenancy policy describing how nullable caller tenant ids are resolved before persistence.</summary>
public enum TenancyMode
{
    /// <summary>Caller tenant is ignored; resolved value is always <see langword="null" /> (system-level row). Only valid for stores backed by a nullable <c>tenant_id</c> column.</summary>
    SystemOnly,

    /// <summary>
    /// Caller-supplied tenant is honoured; when omitted/empty falls back to a default (feature-level then global). Suitable for single-tenant deployments and shared
    /// single-tenant defaults.
    /// </summary>
    SingleTenantDefault,

    /// <summary>Caller must supply a non-empty tenant id; resolution throws when the caller omits the tenant. Used to enforce explicit multi-tenant scoping.</summary>
    MultiTenantStrict,

    /// <summary>Caller-supplied tenant is stored when non-empty; when omitted/empty the row is untenanted (<see langword="null" />). For nullable <c>tenant_id</c> stores that mix tenant-scoped and system-level rows.</summary>
    MultiTenantOptional
}