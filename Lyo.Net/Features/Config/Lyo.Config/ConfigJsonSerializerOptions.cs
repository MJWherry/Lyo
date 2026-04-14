using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Config;

/// <summary>Shared <see cref="JsonSerializerOptions" /> for config defaults, bindings, and deserialization so API and persistence stay aligned.</summary>
public static class ConfigJsonSerializerOptions
{
    /// <summary>Options used when <see cref="ConfigValue" /> callers pass null for options: camelCase, case-insensitive reads, omit nulls when writing.</summary>
    public static JsonSerializerOptions Default { get; } = Create();

    private static JsonSerializerOptions Create()
        => new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
}