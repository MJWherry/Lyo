using System.Text.Json;
using System.Text.Json.Serialization;
#if NETSTANDARD2_0
#pragma warning disable CS8604 // Possible null reference argument.
#endif

namespace Lyo.DateAndTime.Json;

/// <summary><see cref="JsonConverter{T}" /> implementation for <see cref="DateOnlyModel" /> using ISO-8601 calendar dates.</summary>
/// <remarks>Null JSON tokens and empty strings deserialize to <see langword="null" />; writes <c>yyyy-MM-dd</c> for non-null values.</remarks>
public sealed class DateOnlyModelConverter : JsonConverter<DateOnlyModel?>
{
    private const string Format = "yyyy-MM-dd";

    /// <inheritdoc />
    public override DateOnlyModel? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString();
        return string.IsNullOrWhiteSpace(text) ? null : DateOnlyModel.Parse(text);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateOnlyModel? value, JsonSerializerOptions options)
    {
        if (value == null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value.ToString(Format));
    }
}