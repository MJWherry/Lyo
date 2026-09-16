namespace Lyo.Idempotency;

/// <summary>Lifecycle of one idempotency row.</summary>
public enum IdempotencyState
{
    /// <summary>Claimed and not finished. Still occupies the unique key.</summary>
    InProgress,

    /// <summary>Finished with a stored response. Replayed until expiry.</summary>
    Completed,

    /// <summary>Finished with a stored error. Replayed until expiry. The work is not run again.</summary>
    Failed,

    /// <summary>Dropped out of the unique index so the key can be claimed again.</summary>
    Expired
}
