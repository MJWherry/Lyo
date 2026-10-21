namespace Lyo.Idempotency;

/// <summary>Limits and the default lifetime for API claims.</summary>
public sealed class IdempotencyOptions
{
    /// <summary>Default maximum stored scope length.</summary>
    public const int DefaultMaxScopeLength = 1024;

    /// <summary>Default maximum key length.</summary>
    public const int DefaultMaxKeyLength = 255;

    /// <summary>Default cap on a stored response or payload, in bytes.</summary>
    public const int DefaultMaxBodyBytes = 256 * 1024;

    /// <summary>Maximum length of the stored scope, including any outbox prefix.</summary>
    public int MaxScopeLength { get; set; } = DefaultMaxScopeLength;

    /// <summary>Maximum length of the key.</summary>
    public int MaxKeyLength { get; set; } = DefaultMaxKeyLength;

    /// <summary>Responses larger than this are stored as omitted. The work is still marked finished.</summary>
    public int MaxResponseBytes { get; set; } = DefaultMaxBodyBytes;

    /// <summary>Payloads larger than this are rejected before insert.</summary>
    public int MaxPayloadBytes { get; set; } = DefaultMaxBodyBytes;

    /// <summary>How long an API claim stays replayable when the caller does not pass an expiry. Also the lease for API rows, so they are not stolen early.</summary>
    public TimeSpan DefaultLifetime { get; set; } = TimeSpan.FromHours(24);
}