using System.Diagnostics;

namespace Lyo.IO.Temp;

/// <summary>Settings for the <see cref="IOTempCleanupWorker" /> hosted service.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class IOTempCleanupOptions
{
    public const string SectionName = "IOTempCleanup";

    /// <summary>Wait after start before the first cleanup. Default: 5 minutes.</summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Gap between later cleanups. Default: 1 hour.</summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>Delay before the first cleanup and the gap between later ones.</summary>
    public override string ToString() => $"InitialDelay={InitialDelay} Interval={Interval}";
}