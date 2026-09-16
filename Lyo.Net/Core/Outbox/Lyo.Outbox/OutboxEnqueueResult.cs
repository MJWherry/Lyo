namespace Lyo.Outbox;

/// <summary>What <see cref="IOutbox.EnqueueAsync" /> did. A false <see cref="Inserted" /> leaves the caller transaction open.</summary>
public sealed class OutboxEnqueueResult
{
    /// <summary>True when this call inserted the row. False when the same payload was already stored.</summary>
    public bool Inserted { get; init; }

    /// <summary>New or existing row id. Consumers dedupe on this value.</summary>
    public Guid Id { get; init; }
}
