using Lyo.Compression.Models;
using Lyo.Encryption.Models;

namespace Lyo.Cache;

/// <summary>Result of decoding a cached byte payload: optional operation metadata and final plaintext bytes.</summary>
public sealed record CacheEntryEnvelope
{
    /// <summary>Compression metadata when the entry was stored compressed; otherwise null.</summary>
    public CompressionResult? Compression { get; init; }

    /// <summary>Encryption metadata when the entry was stored encrypted; otherwise null.</summary>
    public EncryptionResult? Encryption { get; init; }

    /// <summary>Decoded plaintext application bytes (not JSON).</summary>
    public IReadOnlyList<byte> Payload { get; init; } = [];
}
