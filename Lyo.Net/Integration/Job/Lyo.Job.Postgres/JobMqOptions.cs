namespace Lyo.Job.Postgres;

/// <summary>
/// Message-queue topology options for the job system. Worker run and cancellation queues are declared at host startup (via
/// <see cref="Events.MqJobEventPublisher.SetupAsync" />), not when workers register or subscribe.
/// </summary>
public sealed class JobMqOptions
{
    public const string SectionName = "JobMqOptions";

    /// <summary>
    /// Worker types for which <c>job.run.{workerType}</c> and <c>job.run.{workerType}.cancel</c> queues are declared at startup. Merged with distinct <c>WorkerType</c> values
    /// from <c>JobDefinition</c> rows when the database is available.
    /// </summary>
    public IReadOnlyList<string> WorkerTypes { get; set; } = [];
}