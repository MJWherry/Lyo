namespace Lyo.IO.Temp;

/// <summary>Settings for the <see cref="IOTempCleanupWorker" /> hosted service.</summary>
public sealed class IOTempCleanupOptions
{
    public const string SectionName = "IOTempCleanup";

    /// <summary>Wait after start before the first cleanup. Default: 5 minutes.</summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Gap between later cleanups. Default: 1 hour.</summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(1);
}