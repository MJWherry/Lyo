using Lyo.Exceptions.Models;

namespace Lyo.MessageQueue;

/// <summary>Settings shared by <see cref="QueueWorkerBase{TRequest, TResult}" /> subclasses. Bind from configuration (section <see cref="SectionName" />) or register the instance.</summary>
public sealed class QueueWorkerOptions
{
    public const string SectionName = "QueueWorkerOptions";

    /// <summary>Drain budget used when nothing overrides <see cref="DrainTimeout" />.</summary>
    public static readonly TimeSpan DefaultDrainTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Application-level requeue cap used when the worker constructor omits <c>maxRequeueCount</c>. After the cap, messages go to the
    /// worker's DLQ (or are dropped when no DLQ is set). Null means unlimited retries; the default is 5 so a failing message cannot loop forever by default.
    /// </summary>
    public int? DefaultMaxRequeueCount { get; set; } = 5;

    /// <summary>
    /// Base wait between retries (<see cref="QueueWorkerBase{TRequest, TResult}.RequeueDelay" />), scaled linearly by attempt (attempt 2 waits 2x,
    /// attempt 3 waits 3x, and so on). Needs a delay-capable transport (<see cref="IDelayedMqService" />, for example RabbitMQ); other transports retry immediately. Null or zero turns the
    /// delay off. Default is 2 seconds so a failing message cannot exhaust its retry budget in milliseconds.
    /// </summary>
    public TimeSpan? RequeueDelay { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// How long <see cref="QueueWorkerBase{TRequest, TResult}.StopAsync" /> waits for in-flight messages before it gives up. For job workers this is the window a
    /// long-running run has to finish (or hand itself back) during a graceful shutdown. Defaults to 30 seconds.
    /// </summary>
    public TimeSpan DrainTimeout { get; set; } = DefaultDrainTimeout;

    /// <summary>
    /// Extra time the generic host <c>ShutdownTimeout</c> must allow past <see cref="DrainTimeout" />, covering deregistration and the rest of shutdown. The
    /// host default is well below a full drain, so registration raises <c>ShutdownTimeout</c> to <c>DrainTimeout + ShutdownTimeoutHeadroom</c> when it is lower. Defaults to 15
    /// seconds.
    /// </summary>
    public TimeSpan ShutdownTimeoutHeadroom { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>Checks the options and returns validation failures (empty when valid).</summary>
    public IReadOnlyList<string> GetValidationErrors()
    {
        var errors = new List<string>();
        if (DefaultMaxRequeueCount is < 0)
            errors.Add($"{nameof(DefaultMaxRequeueCount)} must be 0 or greater when set.");

        if (RequeueDelay is { } delay && delay < TimeSpan.Zero)
            errors.Add($"{nameof(RequeueDelay)} must not be negative.");

        if (DrainTimeout <= TimeSpan.Zero)
            errors.Add($"{nameof(DrainTimeout)} must be greater than zero.");

        if (ShutdownTimeoutHeadroom < TimeSpan.Zero)
            errors.Add($"{nameof(ShutdownTimeoutHeadroom)} must not be negative.");

        return errors;
    }

    /// <summary>Checks the options and throws <see cref="ValidationException" /> when invalid.</summary>
    public void Validate()
    {
        var errors = GetValidationErrors();
        if (errors.Count > 0)
            throw new ValidationException($"Invalid {nameof(QueueWorkerOptions)}: {string.Join(" ", errors)}");
    }
}