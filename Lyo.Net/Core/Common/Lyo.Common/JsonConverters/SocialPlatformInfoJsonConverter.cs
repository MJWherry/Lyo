using System.Text.Json;
using System.Text.Json.Serialization;
using Lyo.Common.Extensions;
using Lyo.Common.Records;

namespace Lyo.Common.JsonConverters;

/// <summary>JSON converter for SocialPlatformInfo that serializes/deserializes using slug.</summary>
public class SocialPlatformInfoJsonConverter : JsonConverter<SocialPlatformInfo>
{
    public override SocialPlatformInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return SocialPlatformInfo.Unknown;

        if (reader.TokenType != JsonTokenType.String)
            return SocialPlatformInfo.Unknown;

        var value = reader.GetString();
        if (value.IsNullOrWhitespace())
            return SocialPlatformInfo.Unknown;

        var result = SocialPlatformInfo.FromSlug(value);
        if (result != SocialPlatformInfo.Unknown)
            return result;

        result = SocialPlatformInfo.FromName(value);
        if (result != SocialPlatformInfo.Unknown)
            return result;

        return SocialPlatformInfo.FromAlias(value);
    }

    public override void Write(Utf8JsonWriter writer, SocialPlatformInfo value, JsonSerializerOptions options)
    {
        if (value == SocialPlatformInfo.Unknown) {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Slug);
    }
}
