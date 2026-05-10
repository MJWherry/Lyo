using Lyo.EntityReference.Models;
using Microsoft.Extensions.Options;

namespace Lyo.EntityReference.Postgres;

/// <summary>Shared options, interceptor pipeline, and tenant resolution for PostgreSQL association stores.</summary>
public abstract class EntityRefPostgresStoreBase
{
    /// <summary>Host entity-ref options (default tenant, etc.).</summary>
    protected EntityRefOptions EntityRefOptions { get; }

    /// <summary>Interceptors registered for this store (run in registration order).</summary>
    protected IReadOnlyList<IEntityRefActionInterceptor> Interceptors { get; }

    /// <summary>Creates the base with resolved options and optional interceptors.</summary>
    /// <param name="entityRefOptions">Bound host options (must not be null).</param>
    /// <param name="interceptors">Optional interceptors; null is treated as an empty list.</param>
    protected EntityRefPostgresStoreBase(IOptions<EntityRefOptions> entityRefOptions, IEnumerable<IEntityRefActionInterceptor>? interceptors = null)
    {
        ArgumentNullException.ThrowIfNull(entityRefOptions);
        EntityRefOptions = entityRefOptions.Value;
        Interceptors = interceptors?.ToArray() ?? [];
    }

    /// <summary>Resolves a nullable caller tenant using <see cref="EntityRefOptions.DefaultTenantId"/> when omitted or empty.</summary>
    /// <param name="tenantId">Caller-supplied tenant, if any.</param>
    /// <returns>Concrete tenant id to persist.</returns>
    protected Guid ResolveTenant(Guid? tenantId) => EntityRefPostgresStoreHelpers.ResolveTenantId(tenantId, EntityRefOptions);

    /// <summary>Runs registered interceptors for the given persistence phase.</summary>
    /// <param name="moduleKey">Logical module name passed to interceptors.</param>
    /// <param name="tenantId">Resolved tenant id.</param>
    /// <param name="kind">Lifecycle phase.</param>
    /// <param name="entity">Optional EF entity or payload.</param>
    /// <param name="ct">Cancellation token.</param>
    protected ValueTask RunInterceptorsAsync(string moduleKey, Guid tenantId, EntityRefActionKind kind, object? entity, CancellationToken ct)
        => EntityRefPostgresStoreHelpers.RunInterceptorsAsync(
            Interceptors,
            new EntityRefActionContext { Kind = kind, TenantId = tenantId, ModuleKey = moduleKey, Entity = entity },
            ct);
}
