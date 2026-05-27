using System.Threading;
using System.Threading.Tasks;
using Lyo.Authentication.Models.Audit;

namespace Lyo.Authentication.Audit;

/// <summary>
/// Persistence/sink contract for <see cref="AuthAuditEvent"/>. Always non-throwing — implementations MUST swallow exceptions or log-and-swallow, because audit failures should never
/// bubble up and break the authentication codepath itself. The default <see cref="NullAuthAuditRecorder"/> is registered when nothing else is.
/// </summary>
public interface IAuthAuditRecorder
{
    /// <summary>Records a single event. Implementations may persist synchronously, asynchronously, or fire-and-forget.</summary>
    Task RecordAsync(AuthAuditEvent evt, CancellationToken ct = default);
}
