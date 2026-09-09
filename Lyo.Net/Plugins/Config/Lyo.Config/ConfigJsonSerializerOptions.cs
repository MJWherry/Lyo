using System.Text.Json;
using System.Text.Json.Serialization;
using Lyo.Common.Json;

namespace Lyo.Config;

/// <summary>Common <see cref="JsonSerializerOptions" /> for config defaults, bindings, and deserialization so API and persistence stay aligned.</summary>
public static class ConfigJsonSerializerOptions
{
    /// <summary>Defaults used when <see cref="ConfigValue" /> callers pass null for options: Lyo HTTP JSON defaults plus omit nulls when writing.</summary>
    public static JsonSerializerOptions Default { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = LyoJsonSerializerOptions.Create();
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        return options;
    }
}