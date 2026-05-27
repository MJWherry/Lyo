using Lyo.EntityReference.Models;
using Microsoft.Extensions.Options;

namespace Lyo.EntityReference.Postgres;

/// <summary>Shared options, interceptor pipeline, and tenant resolution for PostgreSQL association stores.</summary>
public abstract class EntityRefPostgresStoreBase
{
    /// <summary>Host entity-ref options (default tenant, mode, etc.).</summary>
    protected EntityRefOptions EntityRefOptions { get; }

    /// <summary>Per-feature tenancy options. Each unset property inherits from <see cref="EntityRefOptions" />.</summary>
    protected TenancyOptions FeatureTenancy { get; }

    /// <summary>Interceptors registered for this store (run in registration order).</summary>
    protected IReadOnlyList<IEntityRefActionInterceptor> Interceptors { get; }

    /// <summary>When <see langword="true" /> the underlying entity column is non-nullable, so <see cref="TenancyMode.SystemOnly" /> is invalid and the ctor will reject it.</summary>
    /// <remarks>Stores derived from <see cref="Database.EntityRefEntityBase" /> leave the default; subclasses on a nullable-tenant entity should pass <see langword="false" /> to the base ctor.</remarks>
    protected bool RequiresNonNullTenant { get; }

    /// <summary>Creates the base with resolved options, per-feature tenancy, and optional interceptors.</summary>
    /// <param name="entityRefOptions">Bound host options (must not be null).</param>
    /// <param name="featureTenancy">Per-feature tenancy options (must not be null - features without overrides should supply <c>new TenancyOptions()</c>).</param>
    /// <param name="interceptors">Optional interceptors; null is treated as an empty list.</param>
    /// <param name="requiresNonNullTenant">When <see langword="true" /> (the default) the underlying tenant column is non-null and <see cref="TenancyMode.SystemOnly" /> is rejected at construction time.</param>
    /// <exception cref="InvalidOperationException">The effective <see cref="TenancyMode" /> resolves to <see cref="TenancyMode.SystemOnly" /> but this store requires a non-null tenant column.</exception>
    protected EntityRefPostgresStoreBase(
        IOptions<EntityRefOptions> entityRefOptions,
        TenancyOptions featureTenancy,
        IEnumerable<IEntityRefActionInterceptor>? interceptors = null,
        bool requiresNonNullTenant = true)
    {
        ArgumentNullException.ThrowIfNull(entityRefOptions);
        ArgumentNullException.ThrowIfNull(featureTenancy);
        EntityRefOptions = entityRefOptions.Value;
        FeatureTenancy = featureTenancy;
        Interceptors = interceptors?.ToArray() ?? [];
        RequiresNonNullTenant = requiresNonNullTenant;

        ValidateTenancyMode();
    }

    /// <summary>Resolves a nullable caller tenant under the feature/global policy and asserts the result is non-null.</summary>
    /// <param name="tenantId">Caller-supplied tenant, if any.</param>
    /// <returns>Concrete tenant id to persist.</returns>
    /// <exception cref="InvalidOperationException">The resolved tenant is null but this store cannot persist null tenants (this would indicate an internal inconsistency since the ctor rejects <see cref="TenancyMode.SystemOnly" /> when <see cref="RequiresNonNullTenant" /> is true).</exception>
    /// <exception cref="ArgumentNullException">Mode is <see cref="TenancyMode.MultiTenantStrict" /> and the caller did not supply a non-empty tenant.</exception>
    protected Guid ResolveTenant(Guid? tenantId)
    {
        var resolved = TenancyResolver.Resolve(tenantId, FeatureTenancy, EntityRefOptions);
        return resolved
            ?? throw new InvalidOperationException(
                $"Store {GetType().Name} resolved a null tenant but is mapped to a non-null TenantId column.");
    }

    /// <summary>Resolves a nullable caller tenant under the feature/global policy, returning <see langword="null" /> when the mode is <see cref="TenancyMode.SystemOnly" />.</summary>
    /// <param name="tenantId">Caller-supplied tenant, if any.</param>
    /// <returns>Resolved tenant id or <see langword="null" /> for system rows.</returns>
    /// <exception cref="ArgumentNullException">Mode is <see cref="TenancyMode.MultiTenantStrict" /> and the caller did not supply a non-empty tenant.</exception>
    protected Guid? ResolveTenantOrNull(Guid? tenantId)
        => TenancyResolver.Resolve(tenantId, FeatureTenancy, EntityRefOptions);

    /// <summary>Runs registered interceptors for the given persistence phase.</summary>
    /// <param name="moduleKey">Logical module name passed to interceptors.</param>
    /// <param name="tenantId">Resolved tenant id.</param>
    /// <param name="kind">Lifecycle phase.</param>
    /// <param name="entity">Optional EF entity or payload.</param>
    /// <param name="ct">Cancellation token.</param>
    protected ValueTask RunInterceptorsAsync(string moduleKey, Guid tenantId, EntityRefActionKind kind, object? entity, CancellationToken ct)
        => EntityRefPostgresStoreHelpers.RunInterceptorsAsync(
            Interceptors, new() {
                Kind = kind,
                TenantId = tenantId,
                ModuleKey = moduleKey,
                Entity = entity
            }, ct);

    private void ValidateTenancyMode()
    {
        if (!RequiresNonNullTenant)
            return;

        var effectiveMode = FeatureTenancy.Mode ?? EntityRefOptions.Mode;
        if (effectiveMode == TenancyMode.SystemOnly)
            throw new InvalidOperationException(
                $"Store {GetType().Name} requires a non-null TenantId column and cannot be configured with TenancyMode.SystemOnly. "
                + "Set Tenancy.Mode to SingleTenantDefault or MultiTenantStrict, or use a store backed by a nullable tenant column.");
    }
}