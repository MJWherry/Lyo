namespace Lyo.Job.Postgres;

/// <summary>
/// Message-queue topology settings for the job system. Worker run queues are declared at host startup (via
/// <see cref="Events.MqJobEventPublisher.SetupAsync" />) and again by each worker for its own type if the broker is still empty.
/// </summary>
public sealed class JobMqOptions
{
    public const string SectionName = "JobMqOptions";

    /// <summary>
    /// Worker types for which <c>job.run.{workerType}</c> and <c>job.run.{workerType}.cancel</c> queues are declared at startup. Combined with distinct <c>WorkerType</c> values
    /// from <c>JobDefinition</c> rows when the database is available.
    /// </summary>
    public IReadOnlyList<string> WorkerTypes { get; set; } = [];

    /// <summary>
    /// Rejects worker-type entries that cannot produce a valid queue name. Invoked by the <c>AddMqJobEventPublisher</c> overloads: a blank or duplicate entry would otherwise
    /// silently declare (or re-declare) the wrong queue at startup.
    /// </summary>
    /// <exception cref="ArgumentException">A worker type is blank, or the same worker type appears twice.</exception>
    public void Validate()
    {
        if (WorkerTypes.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException($"{nameof(WorkerTypes)} contains a blank worker type.", nameof(WorkerTypes));

        var duplicate = WorkerTypes.GroupBy(t => t, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
            throw new ArgumentException($"{nameof(WorkerTypes)} contains duplicate worker type '{duplicate.Key}'.", nameof(WorkerTypes));
    }
}
