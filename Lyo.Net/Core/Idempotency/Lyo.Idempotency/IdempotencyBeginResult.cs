namespace Lyo.Idempotency;

/// <summary>Outcome of <see cref="IIdempotencyStore.TryBeginAsync" />.</summary>
public sealed class IdempotencyBeginResult
{
    /// <summary>How the claim resolved.</summary>
    public required IdempotencyOutcome Outcome { get; init; }

    /// <summary>Row id when one exists.</summary>
    public Guid Id { get; init; }

    /// <summary>Set only for <see cref="IdempotencyOutcome.Began" />. Required to complete, fail, or abandon.</summary>
    public Guid? LeaseToken { get; init; }

    /// <summary>Stored HTTP status for a replay.</summary>
    public int? ResponseStatus { get; init; }

    /// <summary>Stored body for a replay. Null when <see cref="ResponseOmitted" /> is true.</summary>
    public byte[]? ResponseBody { get; init; }

    /// <summary>Stored content type for a replay.</summary>
    public string? ResponseContentType { get; init; }

    /// <summary>True when the finished response was over the cap and the body was not stored.</summary>
    public bool ResponseOmitted { get; init; }
}
