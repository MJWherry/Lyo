using System.Diagnostics;

namespace Lyo.MessageQueue.RabbitMq;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMqOptions";

    public string Host { get; init; } = null!;

    public int Port { get; init; } = 5672;

    public string VirtualHost { get; init; } = "/";

    public string AdminUrl { get; init; } = null!;

    public string Username { get; init; } = null!;

    public string Password { get; init; } = null!;

    /// <summary>Enable metrics collection for message queue operations. Default: false</summary>
    public bool EnableMetrics { get; set; } = false;

    /// <summary>Maximum number of messages that can be processed concurrently per queue. Default: unlimited (0 means no limit). Overridable per queue via <see cref="QueueProcessingLimits" />.</summary>
    public int ProcessingLimit { get; set; } = 0;

    /// <summary>
    /// Per-queue concurrency limits (queue name → max concurrent messages). Applied as broker prefetch + consumer dispatch concurrency + in-process semaphore, so a
    /// queue with limit 1 processes strictly one message at a time while another can run 10 in parallel. Queues not listed fall back to <see cref="ProcessingLimit" />.
    /// </summary>
    public Dictionary<string, int>? QueueProcessingLimits { get; set; }

    /// <summary>Publish messages as persistent (delivery mode 2) so they survive a broker restart on durable queues. Default: true.</summary>
    public bool PersistentMessages { get; set; } = true;

    /// <summary>
    /// Enable publisher confirmations on the publish channel. When on, <c>SendToQueue</c>/<c>SendToExchange</c> only return true once the broker confirms the publish and
    /// return false on nack. Adds a round-trip per publish. Default: false.
    /// </summary>
    public bool PublisherConfirms { get; set; } = false;

    /// <summary>Enable RabbitMQ client automatic connection and topology recovery (reconnects and restores channels/consumers after a network drop). Default: true.</summary>
    public bool AutomaticRecovery { get; set; } = true;

    /// <summary>Time between automatic recovery attempts when <see cref="AutomaticRecovery" /> is enabled. Default: 5 seconds.</summary>
    public TimeSpan NetworkRecoveryInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Number of additional connect attempts made by <c>ConnectAsync</c> when the broker is unreachable (e.g. startup races). 0 = fail on the first error. Default: 3.</summary>
    public int ConnectRetryCount { get; set; } = 3;

    /// <summary>Delay between connect attempts. Default: 2 seconds.</summary>
    public TimeSpan ConnectRetryDelay { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>List of queue names that should be created/initialized on startup. Default: empty.</summary>
    public IReadOnlyList<string>? DefinedQueues { get; set; }

    /// <summary>How to handle exceptions during message processing. Default: RequeueOnException</summary>
    public MessageProcessingExceptionHandling ExceptionHandling { get; set; } = MessageProcessingExceptionHandling.RequeueOnException;

    /// <summary>Resolves the effective concurrency limit for a queue: the per-queue override when present, otherwise the global <see cref="ProcessingLimit" />. 0 = unlimited.</summary>
    public int GetProcessingLimit(string queueName)
        => QueueProcessingLimits != null && QueueProcessingLimits.TryGetValue(queueName, out var limit) ? limit : ProcessingLimit;

    public override string ToString() => $"{Host}:{Port} (Admin {AdminUrl}) VHOST={VirtualHost}, Username={Username}";
}
