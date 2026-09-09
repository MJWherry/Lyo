namespace Lyo.Cache;

/// <summary>
/// Converts application values to plaintext bytes stored by <see cref="ICachePayloadCodec" /> (after optional compression/encryption). Swap in a custom implementation
/// via DI; the default is <see cref="SystemTextJsonCachePayloadSerializer" />.
/// </summary>
public interface ICachePayloadSerializer
{
    /// <summary>Writes <paramref name="value" /> as UTF-8 bytes, or <c>null</c> when <paramref name="value" /> is <c>null</c>.</summary>
    byte[]? Serialize<T>(T? value);

    /// <summary>Reads plaintext UTF-8 bytes produced by <see cref="Serialize{T}" />.</summary>
    T? Deserialize<T>(ReadOnlySpan<byte> utf8Bytes);
}