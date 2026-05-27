using System.Threading;
using System.Threading.Tasks;
using Lyo.Authentication.Models.Audit;

namespace Lyo.Authentication.Audit;

/// <summary>Sentinel <see cref="IAuthAuditRecorder"/> that drops every event on the floor. Registered as the default so that callers can unconditionally inject <see cref="IAuthAuditRecorder"/> without forcing a persistence story onto every host.</summary>
public sealed class NullAuthAuditRecorder : IAuthAuditRecorder
{
    /// <summary>Singleton instance.</summary>
    public static readonly NullAuthAuditRecorder Instance = new();

    /// <inheritdoc/>
    public Task RecordAsync(AuthAuditEvent evt, CancellationToken ct = default) => Task.CompletedTask;
}
