namespace Lyo.MessageQueue;

/// <summary>Options shared by <see cref="QueueWorkerBase{TRequest, TResult}" /> subclasses. Bind from configuration (section <see cref="SectionName" />) or register directly.</summary>
public sealed class QueueWorkerOptions
{
    public const string SectionName = "QueueWorkerOptions";

    /// <summary>
    /// Maximum application-level requeues applied when a worker's constructor does not pass an explicit <c>maxRequeueCount</c>. After the limit, messages are routed to the
    /// worker's DLQ (or dropped when no DLQ is configured). Null = unlimited retries; the default is 5 so failing messages cannot loop forever out of the box.
    /// </summary>
    public int? DefaultMaxRequeueCount { get; set; } = 5;
}
