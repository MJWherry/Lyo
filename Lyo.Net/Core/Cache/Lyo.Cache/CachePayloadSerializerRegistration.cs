using System.Text.Json;

namespace Lyo.Cache;

/// <summary>Default <see cref="ICachePayloadSerializer"/> registration using JSON.</summary>
public static class CachePayloadSerializerRegistration
{
    public static JsonSerializerOptions DefaultJsonOptions { get; } = new() { PropertyNameCaseInsensitive = true };

    public static ICachePayloadSerializer Create(IServiceProvider _)
        => new SystemTextJsonCachePayloadSerializer(DefaultJsonOptions);
}
