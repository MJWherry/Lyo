namespace Lyo.EntityReference.Models;

/// <summary>Optional hook for host-specific rules around entity-ref persistence (audit, tenancy, enrichment).</summary>
public interface IEntityRefActionInterceptor
{
    /// <summary>Invoked for each registered interceptor, in registration order, for the phase described by <see cref="EntityRefActionContext.Kind"/>.</summary>
    /// <param name="context">Phase, tenant, module key, and optional entity payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the interceptor finishes.</returns>
    ValueTask InterceptAsync(EntityRefActionContext context, CancellationToken cancellationToken);
}
