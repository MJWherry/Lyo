namespace Lyo.IO.Temp.Models;

/// <summary>Aggregate statistics for a running <see cref="IOTempService" /> instance.</summary>
public record IOTempServiceStats(
    /// <summary>Number of currently tracked (non-disposed) sessions.</summary>
    int ActiveSessionCount,
    /// <summary>Number of sessions created via <see cref="IIOTempService.GetOrCreateSession"/>.</summary>
    int KeyedSessionCount,
    /// <summary>Sum of bytes used across all active sessions.</summary>
    long TotalBytesUsed,
    /// <summary>The service's own temp directory path.</summary>
    string ServiceDirectory);