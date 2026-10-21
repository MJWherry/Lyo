namespace Lyo.Idempotency;

/// <summary>Result of trying to claim a scope and key.</summary>
public enum IdempotencyOutcome
{
    /// <summary>This caller owns the new in-progress row.</summary>
    Began,

    /// <summary>The same hash already finished. The stored response is on the result.</summary>
    Replay,

    /// <summary>The same hash is still in progress. Do not run the work and do not steal the row.</summary>
    InProgress,

    /// <summary>The key is held by a different hash, or by a finished row whose expiry has passed and has not been swept.</summary>
    Conflict
}