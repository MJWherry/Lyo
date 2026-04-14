using System.Text.Json;

namespace Lyo.Cache;

/// <summary>Default <see cref="ICachePayloadSerializer"/> using <see cref="JsonSerializer"/>.</summary>
public sealed class SystemTextJsonCachePayloadSerializer(JsonSerializerOptions options) : ICachePayloadSerializer
{
    public byte[]? Serialize<T>(T? value)
        => value is null ? null : JsonSerializer.SerializeToUtf8Bytes(value, options);

    public T? Deserialize<T>(ReadOnlySpan<byte> utf8Bytes)
        => JsonSerializer.Deserialize<T>(utf8Bytes, options);
}
