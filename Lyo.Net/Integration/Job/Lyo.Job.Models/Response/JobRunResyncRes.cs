namespace Lyo.Job.Models.Response;

/// <summary>
/// Result of <c>POST Job/Run/Resync</c>: how many due <c>Queued</c> runs were considered, already present on a worker queue, republished, or failed to publish.
/// </summary>
public sealed record JobRunResyncRes
{
    /// <summary>Due <c>Queued</c> runs considered (not dry-run, slot due or unset).</summary>
    public int Queued { get; init; }

    /// <summary>Runs whose id was already on <c>job.run.{workerType}</c> or its <c>.wait</c> delay queue.</summary>
    public int AlreadyInQueue { get; init; }

    /// <summary>Runs whose dispatch message was republished.</summary>
    public int Republished { get; init; }

    /// <summary>Runs that should have been republished but <c>PublishRunCreatedAsync</c> threw.</summary>
    public int Failed { get; init; }
}
