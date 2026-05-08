using System.Text.Json;

namespace Lyo.Cache;

/// <summary>Default <see cref="ICachePayloadSerializer" /> registration using JSON.</summary>
public static class CachePayloadSerializerRegistration
{
    /// <summary>Shared <see cref="JsonSerializerOptions" /> used by <see cref="Create" /> (case-insensitive property names).</summary>
    public static JsonSerializerOptions DefaultJsonOptions { get; } = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Factory that constructs <see cref="SystemTextJsonCachePayloadSerializer" /> with <see cref="DefaultJsonOptions" />.</summary>
    public static ICachePayloadSerializer Create(IServiceProvider _) => new SystemTextJsonCachePayloadSerializer(DefaultJsonOptions);
}