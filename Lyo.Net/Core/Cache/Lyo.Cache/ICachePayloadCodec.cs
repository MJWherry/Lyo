namespace Lyo.Cache;

/// <summary>Encodes plaintext bytes to a framed cache blob and decodes back to <see cref="CacheEntryEnvelope"/>.</summary>
public interface ICachePayloadCodec
{
    /// <summary>Applies configured compress/encrypt and returns a framed byte array for storage.</summary>
    byte[] Encode(ReadOnlySpan<byte> plaintext);

    /// <summary>
    /// Same as <see cref="Encode"/> for storage, but also returns a <see cref="CacheEntryEnvelope"/> whose
    /// <see cref="CacheEntryEnvelope.Payload"/> is the original plaintext without a decompress round-trip (for cache misses).
    /// </summary>
    (byte[] Framed, CacheEntryEnvelope Envelope) EncodeReturningEnvelope(ReadOnlySpan<byte> plaintext);

    /// <summary>Parses a framed blob from the cache and returns plaintext plus optional metadata.</summary>
    CacheEntryEnvelope Decode(byte[] framed);
}
