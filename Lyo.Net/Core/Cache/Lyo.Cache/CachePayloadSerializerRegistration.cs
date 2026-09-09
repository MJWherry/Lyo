using System.Text.Json;

namespace Lyo.Cache;

/// <summary>Default JSON registration for <see cref="ICachePayloadSerializer" />.</summary>
public static class CachePayloadSerializerRegistration
{
    /// <summary>Shared <see cref="JsonSerializerOptions" /> used by <see cref="Create" /> (property names are case-insensitive).</summary>
    public static JsonSerializerOptions DefaultJsonOptions { get; } = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Factory that builds <see cref="SystemTextJsonCachePayloadSerializer" /> with <see cref="DefaultJsonOptions" />.</summary>
    public static ICachePayloadSerializer Create(IServiceProvider _) => new SystemTextJsonCachePayloadSerializer(DefaultJsonOptions);
}