using System.Text.Json;
using System.Text.Json.Serialization;

#if NETSTANDARD2_0
#pragma warning disable CS8604 // Possible null reference argument.
#endif

namespace Lyo.DateAndTime.Json;

/// <summary>JSON converter for TimeOnlyModel that handles null values properly.</summary>
public sealed class TimeOnlyModelConverter : JsonConverter<TimeOnlyModel?>
{
    private const string Format = "HH:mm:ss.fffffff"; // matches .NET's default

    /// <summary>Reads a TimeOnlyModel from JSON.</summary>
    /// <param name="reader">The JSON reader</param>
    /// <param name="typeToConvert">The type to convert</param>
    /// <param name="options">The serializer options</param>
    /// <returns>The parsed TimeOnlyModel or null if the value is null or whitespace</returns>
    public override TimeOnlyModel? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString();
        return string.IsNullOrWhiteSpace(text) ? null : TimeOnlyModel.Parse(text);
    }

    /// <summary>Writes a TimeOnlyModel to JSON.</summary>
    /// <param name="writer">The JSON writer</param>
    /// <param name="value">The TimeOnlyModel value to write</param>
    /// <param name="options">The serializer options</param>
    public override void Write(Utf8JsonWriter writer, TimeOnlyModel? value, JsonSerializerOptions options)
    {
        if (value == null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value.ToString(Format));
    }
}