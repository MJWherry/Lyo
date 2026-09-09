namespace Lyo.MessageQueue;

/// <summary>Queue-message envelope with requeue tracking, tracing, and metadata.</summary>
/// <typeparam name="T">Payload type.</typeparam>
/// <param name="Payload">Request or message body.</param>
/// <param name="RequeueCount">How many times this message has been requeued (for max-requeue caps).</param>
/// <param name="MessageId">Stable id across the message lifetime, including requeues. Use in logs and debugging.</param>
/// <param name="EnqueuedAt">First enqueue time. Use for staleness checks and metrics.</param>
/// <param name="TraceId">Distributed trace id that ties processing back to the original request.</param>
/// <param name="Version">Envelope schema version, so the format can evolve.</param>
public sealed record QueueMessageEnvelope<T>(T Payload, int RequeueCount = 0, string? MessageId = null, DateTime? EnqueuedAt = null, string? TraceId = null, int Version = 1);