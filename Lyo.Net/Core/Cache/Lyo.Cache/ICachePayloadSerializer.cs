namespace Lyo.Cache;

/// <summary>
/// Converts application values to plaintext bytes stored by <see cref="ICachePayloadCodec"/> (after optional compression/encryption).
/// Replace with a custom implementation via DI; the default is <see cref="SystemTextJsonCachePayloadSerializer"/>.
/// </summary>
public interface ICachePayloadSerializer
{
    /// <summary>Serializes <paramref name="value"/> to UTF-8 bytes, or <c>null</c> when <paramref name="value"/> is <c>null</c>.</summary>
    byte[]? Serialize<T>(T? value);

    /// <summary>Deserializes plaintext UTF-8 bytes produced by <see cref="Serialize{T}"/>.</summary>
    T? Deserialize<T>(ReadOnlySpan<byte> utf8Bytes);
}
