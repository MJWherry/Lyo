using System.Diagnostics;
using Lyo.Common.Metadata.Records;

namespace Lyo.MessageQueue.RabbitMq;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMqOptions";

    public string Host { get; init; } = null!;

    /// <summary>AMQP port. Defaults to <see cref="PortInfo.Amqp" /> (5672).</summary>
    public int Port { get; init; } = PortInfo.Amqp;

    public string VirtualHost { get; init; } = "/";

    public string AdminUrl { get; init; } = null!;

    public string Username { get; init; } = null!;

    public string Password { get; init; } = null!;

    /// <summary>When true, collect metrics for queue operations. Default: false</summary>
    public bool EnableMetrics { get; set; } = false;

    /// <summary>
    /// How many messages a queue may process at once. Default: unlimited (0). Override per queue with
    /// <see cref="QueueProcessingLimits" />.
    /// </summary>
    public int ProcessingLimit { get; set; } = 0;

    /// <summary>
    /// Concurrency cap per queue (name → max in-flight messages). Enforced as broker prefetch, consumer dispatch concurrency, and an in-process semaphore, so a queue with
    /// limit 1 handles one message at a time while another can run 10 in parallel. Unlisted queues use <see cref="ProcessingLimit" />.
    /// </summary>
    public Dictionary<string, int>? QueueProcessingLimits { get; set; }

    /// <summary>When true, publish with delivery mode 2 so messages survive a broker restart on durable queues. Default: true.</summary>
    public bool PersistentMessages { get; set; } = true;

    /// <summary>
    /// When true, the publish channel uses publisher confirms. <c>SendToQueue</c>/<c>SendToExchange</c> return true only after the broker confirms, and
    /// false on nack. Costs a round-trip per publish. Default: false.
    /// </summary>
    public bool PublisherConfirms { get; set; } = false;

    /// <summary>When true, the RabbitMQ client reconnects and restores channels/consumers after a network drop. Default: true.</summary>
    public bool AutomaticRecovery { get; set; } = true;

    /// <summary>How long to wait between recovery attempts when <see cref="AutomaticRecovery" /> is on. Default: 5 seconds.</summary>
    public TimeSpan NetworkRecoveryInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Extra <c>ConnectAsync</c> attempts when the broker is down (for example at startup). 0 = fail on the first error. Default: 3.</summary>
    public int ConnectRetryCount { get; set; } = 3;

    /// <summary>Wait between connect attempts. Default: 2 seconds.</summary>
    public TimeSpan ConnectRetryDelay { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>Queue names to declare at startup. Default: empty.</summary>
    public IReadOnlyList<string>? DefinedQueues { get; set; }

    /// <summary>What to do when a handler throws. Default: RequeueOnException</summary>
    public MessageProcessingExceptionHandling ExceptionHandling { get; set; } = MessageProcessingExceptionHandling.RequeueOnException;

    /// <summary>Effective concurrency for a queue: the per-queue override if set, otherwise <see cref="ProcessingLimit" />. 0 = unlimited.</summary>
    public int GetProcessingLimit(string queueName) => QueueProcessingLimits != null && QueueProcessingLimits.TryGetValue(queueName, out var limit) ? limit : ProcessingLimit;

    public override string ToString() => $"{Host}:{Port} (Admin {AdminUrl}) VHOST={VirtualHost}, Username={Username}";
}