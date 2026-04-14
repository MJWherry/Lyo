using System.Text.Json;
using System.Text.Json.Serialization;

#if NETSTANDARD2_0
#pragma warning disable CS8604 // Possible null reference argument.
#endif

namespace Lyo.DateAndTime.Json;

/// <summary>JSON converter for DateOnlyModel that handles null values properly.</summary>
public sealed class DateOnlyModelConverter : JsonConverter<DateOnlyModel?>
{
    private const string Format = "yyyy-MM-dd";

    /// <summary>Reads a DateOnlyModel from JSON.</summary>
    /// <param name="reader">The JSON reader</param>
    /// <param name="typeToConvert">The type to convert</param>
    /// <param name="options">The serializer options</param>
    /// <returns>The parsed DateOnlyModel or null if the value is null or whitespace</returns>
    public override DateOnlyModel? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString();
        return string.IsNullOrWhiteSpace(text) ? null : DateOnlyModel.Parse(text);
    }

    /// <summary>Writes a DateOnlyModel to JSON.</summary>
    /// <param name="writer">The JSON writer</param>
    /// <param name="value">The DateOnlyModel value to write</param>
    /// <param name="options">The serializer options</param>
    public override void Write(Utf8JsonWriter writer, DateOnlyModel? value, JsonSerializerOptions options)
    {
        if (value == null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value.ToString(Format));
    }
}