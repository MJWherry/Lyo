using System.Diagnostics;
using Lyo.Compression.Models;
using Lyo.Encryption.Models;

namespace Lyo.Cache;

/// <summary>Decoded cached byte payload: optional operation metadata plus final plaintext bytes.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record CacheEntryEnvelope(byte[] Payload, CompressionResult? Compression = null, EncryptionResult? Encryption = null)
{
    public override string ToString() => $"Size={Payload.Length}, Compression={Compression}, Encryption={Encryption}";
}