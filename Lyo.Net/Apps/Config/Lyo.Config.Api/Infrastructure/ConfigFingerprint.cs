using System.Text.Json;

namespace Lyo.Config.Api.Infrastructure;

internal static class ConfigFingerprint
{
    internal static byte[] CanonicalUtf8(ResolvedConfigRecord resolved)
    {
        var sorted = resolved.Items.OrderBy(i => i.Definition.Key, StringComparer.Ordinal).ToList();
        using var ms = new MemoryStream();
        using (var writer = new Utf8JsonWriter(ms, new() { Indented = false })) {
            writer.WriteStartObject();
            writer.WriteString("entityType", resolved.ForEntityType);
            writer.WriteString("entityId", resolved.ForEntityId);
            writer.WriteStartArray("items");
            foreach (var it in sorted) {
                writer.WriteStartObject();
                writer.WriteString("definitionId", it.Definition.Id);
                writer.WriteString("definitionKey", it.Definition.Key);
                writer.WriteString("definitionForValueType", it.Definition.ForValueType);
                writer.WriteBoolean("definitionIsRequired", it.Definition.IsRequired);
                WriteDateUtc(writer, "definitionCreatedUtc", it.Definition.CreatedTimestamp);
                WriteNullableDateUtc(writer, "definitionUpdatedUtc", it.Definition.UpdatedTimestamp);
                if (it.Binding != null) {
                    writer.WriteString("bindingId", it.Binding.Id);
                    WriteDateUtc(writer, "bindingCreatedUtc", it.Binding.CreatedTimestamp);
                    WriteNullableDateUtc(writer, "bindingUpdatedUtc", it.Binding.UpdatedTimestamp);
                }
                else {
                    writer.WriteNull("bindingId");
                    writer.WriteNull("bindingCreatedUtc");
                    writer.WriteNull("bindingUpdatedUtc");
                }

                WriteTypedJson(writer, "effectiveValue", it.Value?.TypeName, it.Value?.Json);
                WriteTypedJson(writer, "definitionDefaultSource", it.Definition.DefaultValue?.TypeName, it.Definition.DefaultValue?.Json);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return ms.ToArray();
    }

    internal static string ComputeQuotedStrongEtag(ReadOnlySpan<byte> canonicalUtf8)
    {
        var hash = SHA256.HashData(canonicalUtf8);
        var hex = Convert.ToHexString(hash);
        return '"' + hex + '"';
    }

    private static void WriteDateUtc(Utf8JsonWriter writer, string name, DateTime utc)
        => writer.WriteString(name, DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToString("O", CultureInfo.InvariantCulture));

    private static void WriteNullableDateUtc(Utf8JsonWriter writer, string name, DateTime? utc)
    {
        if (!utc.HasValue)
            writer.WriteNull(name);
        else
            WriteDateUtc(writer, name, utc.Value);
    }

    private static void WriteTypedJson(Utf8JsonWriter writer, string propertyName, string? typeName, string? json)
    {
        writer.WritePropertyName(propertyName);
        writer.WriteStartObject();
        if (typeName != null || json != null) {
            writer.WriteString("typeName", typeName ?? string.Empty);
            writer.WriteString("json", json ?? "null");
        }
        else {
            writer.WriteNull("typeName");
            writer.WriteNull("json");
        }

        writer.WriteEndObject();
    }
}