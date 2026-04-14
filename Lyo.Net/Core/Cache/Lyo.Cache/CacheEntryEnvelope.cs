using Lyo.Compression.Models;

namespace Lyo.Cache;

/// <summary>Result of decoding a cached byte payload: optional operation metadata and final plaintext bytes.</summary>
public sealed record CacheEntryEnvelope
{
    /// <summary>Compression metadata when the entry was stored compressed; otherwise null.</summary>
    public CompressionResult? Compression { get; init; }

#if NET10_0_OR_GREATER
    /// <summary>Encryption metadata when the entry was stored encrypted; otherwise null.</summary>
    public Lyo.Encryption.Models.EncryptionResult? Encryption { get; init; }
#endif

    /// <summary>Decoded plaintext application bytes (not JSON).</summary>
    public IReadOnlyList<byte> Payload { get; init; } = [];
}
