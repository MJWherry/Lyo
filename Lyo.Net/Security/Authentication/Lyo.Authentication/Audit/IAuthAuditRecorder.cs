using Lyo.Authentication.Models.Audit;

namespace Lyo.Authentication.Audit;

/// <summary>
/// Persistence/sink contract for <see cref="AuthAuditEvent" />. Never throws — implementations MUST swallow exceptions or log-and-swallow, because audit failures
/// must not bubble into the authentication path. The default <see cref="NullAuthAuditRecorder" /> is registered when nothing else is.
/// </summary>
public interface IAuthAuditRecorder
{
    /// <summary>Records one event. Implementations may persist synchronously, asynchronously, or fire-and-forget.</summary>
    Task RecordAsync(AuthAuditEvent evt, CancellationToken ct = default);
}