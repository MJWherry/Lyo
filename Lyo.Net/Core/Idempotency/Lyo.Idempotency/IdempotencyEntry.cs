namespace Lyo.Idempotency;

/// <summary>One stored claim. Mutable so the in-memory store can finish it under its lock.</summary>
internal sealed class IdempotencyEntry
{
    public Guid Id { get; set; }

    public IdempotencyKind Kind { get; set; }

    public string Scope { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public byte[] RequestHash { get; set; } = [];

    public IdempotencyState State { get; set; }

    public Guid? LeaseToken { get; set; }

    public DateTime? LeaseUntil { get; set; }

    public int? ResponseStatus { get; set; }

    public byte[]? ResponseBody { get; set; }

    public string? ResponseContentType { get; set; }

    public bool ResponseOmitted { get; set; }

    public string? Destination { get; set; }

    public byte[]? Payload { get; set; }

    public int AttemptCount { get; set; }

    public DateTime? AvailableAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? CompletedTimestamp { get; set; }

    public bool IsLive => State is IdempotencyState.InProgress or IdempotencyState.Completed or IdempotencyState.Failed;
}