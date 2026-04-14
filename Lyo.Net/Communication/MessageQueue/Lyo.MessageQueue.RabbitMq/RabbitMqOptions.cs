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

    /// <summary>Maximum number of messages that can be processed concurrently per queue. Default: unlimited (0 means no limit).</summary>
    public int ProcessingLimit { get; set; } = 0;

    /// <summary>List of queue names that should be created/initialized on startup. Default: empty.</summary>
    public IReadOnlyList<string>? DefinedQueues { get; set; }

    /// <summary>How to handle exceptions during message processing. Default: RequeueOnException</summary>
    public MessageProcessingExceptionHandling ExceptionHandling { get; set; } = MessageProcessingExceptionHandling.RequeueOnException;

    public override string ToString() => $"{Host}:{Port} (Admin {AdminUrl}) VHOST={VirtualHost}, Username={Username}";
}