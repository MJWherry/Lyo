namespace Lyo.KeyStore;

/// <summary>Metadata attached to a key version in the store.</summary>
public record KeyMetadata
{
    /// <summary>When the key was created or added to the store.</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Optional expiration for the key. Null means it does not expire.</summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>Algorithm identifier (e.g., "AES-256", "ChaCha20-Poly1305").</summary>
    public string? Algorithm { get; init; }

    /// <summary>Additional custom metadata.</summary>
    public Dictionary<string, string>? AdditionalData { get; init; }

    /// <summary>Whether the key has expired.</summary>
    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;

    /// <summary>Whether the key is still valid (not expired).</summary>
    public bool IsValid => !IsExpired;
}