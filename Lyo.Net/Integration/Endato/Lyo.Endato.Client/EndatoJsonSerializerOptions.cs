using System.Text.Json;
using Lyo.Common.Json;
using Lyo.Common.Json.JsonConverters;

namespace Lyo.Endato.Client;

/// <summary>JSON options for Endato wire shapes (0/1 booleans, etc.).</summary>
public static class EndatoJsonSerializerOptions
{
    /// <summary>Builds Lyo HTTP JSON defaults plus Endato-specific converters.</summary>
    public static JsonSerializerOptions Create()
    {
        var options = LyoJsonSerializerOptions.Create();
        options.Converters.Add(new StringIntBoolConverter());
        options.Converters.Add(new StringIntBoolNullableConverter());
        return options;
    }
}