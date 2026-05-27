using System;
using System.Diagnostics;

namespace Lyo.Diagnostic.Correlation;

/// <summary>
/// Host-agnostic default <see cref="ICorrelationIdResolver"/>. Looks at <see cref="Activity.Current"/> first (so consumers participating in W3C trace context get the same id their
/// upstream caller used) and otherwise mints a fresh hex GUID. Registered as the fallback impl in DI via <c>TryAdd</c> so the ASP.NET-aware impl wins when both packages are present.
/// </summary>
public sealed class AmbientCorrelationIdResolver : ICorrelationIdResolver
{
    /// <summary>Singleton instance — the resolver holds no state.</summary>
    public static readonly AmbientCorrelationIdResolver Instance = new();

    /// <inheritdoc/>
    public string Resolve()
    {
        var activityId = Activity.Current?.Id;
        if (!string.IsNullOrWhiteSpace(activityId))
            return activityId!;

        return Guid.NewGuid().ToString("N");
    }
}
