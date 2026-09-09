namespace Lyo.Job.Models.Response;

/// <summary>Aggregated statistics for a job definition, computed over a rolling window (30 days by default).</summary>
public sealed record JobDefinitionStatsRes
{
    public Guid JobDefinitionId { get; init; }

    /// <summary>How many runs fall inside the statistics window.</summary>
    public int TotalRuns { get; init; }

    /// <summary>Runs whose result is <c>Success</c>, <c>SuccessWithWarnings</c>, or <c>PartialSuccess</c>.</summary>
    public int SuccessCount { get; init; }

    /// <summary>Runs whose result is <c>Failure</c>.</summary>
    public int FailureCount { get; init; }

    /// <summary>Percent of successful runs (0–100). Null when <see cref="TotalRuns" /> is 0.</summary>
    public double? SuccessRate { get; init; }

    /// <summary>Average execution time in milliseconds (start to finish). Null when no finished runs exist.</summary>
    public double? AvgDurationMs { get; init; }

    /// <summary>95th-percentile execution time, in milliseconds. Null when fewer than 20 finished runs exist.</summary>
    public double? P95DurationMs { get; init; }

    /// <summary>UTC time of the most recent run (any state).</summary>
    public DateTime? LastRunAt { get; init; }

    /// <summary>UTC time of the most recent successful run.</summary>
    public DateTime? LastSuccessAt { get; init; }

    /// <summary>Current consecutive failure streak at query time. Reset to 0 when a successful run completes.</summary>
    public int ConsecutiveFailures { get; init; }

    /// <summary>Runs now in <c>Running</c> (not limited to the statistics window).</summary>
    public int RunningCount { get; init; }

    /// <summary>Runs now in <c>Queued</c> (not limited to the statistics window).</summary>
    public int QueuedCount { get; init; }

    /// <summary>How many days the statistics window covers.</summary>
    public int WindowDays { get; init; }
}