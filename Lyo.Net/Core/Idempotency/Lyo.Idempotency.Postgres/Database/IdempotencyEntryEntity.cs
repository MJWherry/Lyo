namespace Lyo.Idempotency.Postgres.Database;

/// <summary>Row in <c>idempotency.entry</c>.</summary>
public sealed class IdempotencyEntryEntity
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary><c>api</c> or <c>outbox</c>.</summary>
    public string Kind { get; set; } = "api";

    /// <summary>Caller boundary. Not null.</summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>Caller key. Not null.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>SHA-256 of the request or payload.</summary>
    public byte[] RequestHash { get; set; } = [];

    /// <summary><c>in_progress</c>, <c>completed</c>, <c>failed</c>, or <c>expired</c>.</summary>
    public string State { get; set; } = "in_progress";

    /// <summary>Token required to finish the row.</summary>
    public Guid? LeaseToken { get; set; }

    /// <summary>When an outbox lease can be stolen. API rows use the expiry.</summary>
    public DateTime? LeaseUntil { get; set; }

    /// <summary>Stored status code.</summary>
    public int? ResponseStatus { get; set; }

    /// <summary>Stored body. Null when omitted or unfinished.</summary>
    public byte[]? ResponseBody { get; set; }

    /// <summary>Stored content type.</summary>
    public string? ResponseContentType { get; set; }

    /// <summary>True when the body was over the cap.</summary>
    public bool ResponseOmitted { get; set; }

    /// <summary>Queue name for outbox rows.</summary>
    public string? Destination { get; set; }

    /// <summary>Outbox payload. The hash is of these bytes.</summary>
    public byte[]? Payload { get; set; }

    /// <summary>Publish attempts.</summary>
    public int AttemptCount { get; set; }

    /// <summary>Do not claim before this time.</summary>
    public DateTime? AvailableAt { get; set; }

    /// <summary>When an API row stops replaying.</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>When the row was inserted.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>When the row was completed or failed.</summary>
    public DateTime? CompletedTimestamp { get; set; }
}
