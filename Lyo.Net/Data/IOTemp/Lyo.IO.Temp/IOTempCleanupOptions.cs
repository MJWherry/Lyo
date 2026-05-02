namespace Lyo.IO.Temp;

/// <summary>Configuration for the <see cref="IOTempCleanupWorker" /> background service.</summary>
public sealed class IOTempCleanupOptions
{
    public const string SectionName = "IOTempCleanup";

    /// <summary>How long to wait after startup before the first cleanup run. Default: 5 minutes.</summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>How often to run cleanup after the initial run. Default: 1 hour.</summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(1);
}