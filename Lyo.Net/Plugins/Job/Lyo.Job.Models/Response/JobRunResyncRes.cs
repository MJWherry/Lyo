namespace Lyo.Job.Models.Response;

/// <summary>
/// Result of <c>POST Job/Run/Resync</c>: how many due <c>Queued</c> runs were considered, already on a worker queue, republished, or failed to publish.
/// </summary>
public sealed record JobRunResyncRes
{
    /// <summary>Due <c>Queued</c> runs considered (not a dry run; slot due or unset).</summary>
    public int Queued { get; init; }

    /// <summary>Runs whose id was already on <c>job.run.{workerType}</c> or that queue's <c>.wait</c> delay companion.</summary>
    public int AlreadyInQueue { get; init; }

    /// <summary>Runs whose dispatch message was published again.</summary>
    public int Republished { get; init; }

    /// <summary>Runs that should have been published again but <c>PublishRunCreatedAsync</c> threw.</summary>
    public int Failed { get; init; }

    /// <summary>
    /// True when more due <c>Queued</c> runs existed than the resync batch limit, so only the oldest batch was considered. Call the endpoint again to keep going.
    /// </summary>
    public bool Truncated { get; init; }
}
