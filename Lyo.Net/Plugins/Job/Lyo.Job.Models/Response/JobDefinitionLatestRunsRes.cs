namespace Lyo.Job.Models.Response;

/// <summary>
/// Latest-run snapshot for one job definition, returned by the batch <c>POST Job/Definition/LatestRuns</c> endpoint. Used by the scheduler to refresh its in-memory
/// cache in one round trip instead of three queries per definition.
/// </summary>
public sealed record JobDefinitionLatestRunsRes
{
    public Guid JobDefinitionId { get; init; }

    /// <summary>Most recent run by creation time, any outcome.</summary>
    public JobRunRes? LastRun { get; init; }

    /// <summary>Most recent run with result Success or SuccessWithWarnings.</summary>
    public JobRunRes? LastSuccessfulRun { get; init; }

    /// <summary>Most recent run with result Failure.</summary>
    public JobRunRes? LastFailedRun { get; init; }
}