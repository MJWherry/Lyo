namespace Lyo.Idempotency;

/// <summary>Which caller wrote the row. API callers cannot create outbox rows.</summary>
public enum IdempotencyKind
{
    /// <summary>HTTP or direct <c>TryBegin</c> claim. Not stolen before expiry.</summary>
    Api,

    /// <summary>Outbox message. A dispatcher may take the lease after <c>lease_until</c>.</summary>
    Outbox
}
