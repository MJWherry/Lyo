using System.Text.Json;
using System.Text.Json.Serialization;

#if NETSTANDARD2_0
#pragma warning disable CS8604 // Possible null reference argument.
#endif

namespace Lyo.DateAndTime.Json;

/// <summary><see cref="JsonConverter{T}" /> for <see cref="TimeOnlyModel" /> using extended time-of-day strings.</summary>
/// <remarks>Null JSON tokens and empty strings deserialize to <see langword="null" />; writes include fractional seconds to match BCL defaults.</remarks>
public sealed class TimeOnlyModelConverter : JsonConverter<TimeOnlyModel?>
{
    private const string Format = "HH:mm:ss.fffffff"; // matches .NET's default

    /// <inheritdoc />
    public override TimeOnlyModel? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString();
        return string.IsNullOrWhiteSpace(text) ? null : TimeOnlyModel.Parse(text);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TimeOnlyModel? value, JsonSerializerOptions options)
    {
        if (value == null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value.ToString(Format));
    }
}