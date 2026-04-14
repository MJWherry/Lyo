namespace Lyo.MessageQueue;

/// <summary>Envelope for queue messages that supports requeue tracking, tracing, and metadata.</summary>
/// <typeparam name="T">The type of the payload.</typeparam>
/// <param name="Payload">The actual request/message data.</param>
/// <param name="RequeueCount">Number of times this message has been requeued. Used for max requeue limits.</param>
/// <param name="MessageId">Stable ID for the message across its lifecycle (including requeues). Use for logging and debugging.</param>
/// <param name="EnqueuedAt">When the message was first enqueued. Use for staleness checks and metrics.</param>
/// <param name="TraceId">Distributed trace ID to correlate processing with the original request.</param>
/// <param name="Version">Schema version for the envelope format. Enables future evolution.</param>
public sealed record QueueMessageEnvelope<T>(T Payload, int RequeueCount = 0, string? MessageId = null, DateTime? EnqueuedAt = null, string? TraceId = null, int Version = 1);