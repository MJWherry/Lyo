namespace Lyo.Cache;

/// <summary>Frames plaintext bytes for storage and decodes them back to <see cref="CacheEntryEnvelope" />.</summary>
public interface ICachePayloadCodec
{
    /// <summary>Applies configured compress/encrypt and returns the framed bytes to store.</summary>
    byte[] Encode(ReadOnlySpan<byte> plaintext);

    /// <summary>
    /// Same as <see cref="Encode" /> for storage, but also returns a <see cref="CacheEntryEnvelope" /> whose <see cref="CacheEntryEnvelope.Payload" /> is the original plaintext
    /// without a decompress round-trip (used on cache misses).
    /// </summary>
    (byte[] Framed, CacheEntryEnvelope Envelope) EncodeReturningEnvelope(ReadOnlySpan<byte> plaintext);

    /// <summary>Parses a framed cache blob and returns plaintext plus optional metadata.</summary>
    CacheEntryEnvelope Decode(byte[] framed);

    /// <summary>True when <paramref name="data" /> is a LYO1 framed blob from <see cref="Encode" />.</summary>
    bool IsFramed(ReadOnlySpan<byte> data);
}