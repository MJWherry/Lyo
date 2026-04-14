namespace Lyo.Cache;

/// <summary>Encodes plaintext bytes to a framed cache blob and decodes back to <see cref="CacheEntryEnvelope"/>.</summary>
public interface ICachePayloadCodec
{
    /// <summary>Applies configured compress/encrypt and returns a framed byte array for storage.</summary>
    byte[] Encode(ReadOnlySpan<byte> plaintext);

    /// <summary>Parses a framed blob from the cache and returns plaintext plus optional metadata.</summary>
    CacheEntryEnvelope Decode(byte[] framed);
}
