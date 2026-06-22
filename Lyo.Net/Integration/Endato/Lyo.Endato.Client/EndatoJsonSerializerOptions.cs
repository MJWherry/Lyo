using System.Text.Json;
using Lyo.Common;
using Lyo.Common.JsonConverters;

namespace Lyo.Endato.Client;

/// <summary>JSON options for Endato API wire shapes (0/1 booleans, etc.).</summary>
public static class EndatoJsonSerializerOptions
{
    /// <summary>Creates Lyo HTTP JSON defaults plus Endato-specific converters.</summary>
    public static JsonSerializerOptions Create()
    {
        var options = LyoJsonSerializerOptions.Create();
        options.Converters.Add(new StringIntBoolConverter());
        options.Converters.Add(new StringIntBoolNullableConverter());
        return options;
    }
}
