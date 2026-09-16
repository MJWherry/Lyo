using System.Text.Json;
using Lyo.Exceptions;

namespace Lyo.FileTags;

/// <summary>Merges a <see cref="FileTags" /> bag into the opaque file-storage metadata object as <c>fileTags</c>.</summary>
public static class FileTagMetadata
{
    /// <summary>Property name on <c>FileStoreResult.Metadata</c>. Other caller keys stay beside it.</summary>
    public const string PropertyName = "fileTags";

    /// <summary>
    /// Copies <paramref name="caller" /> when it is a JSON object, drops any previous <c>fileTags</c> property, and writes <paramref name="tags" /> under <see cref="PropertyName" />.
    /// A null or non-object caller becomes an object that holds only <c>fileTags</c>. Ids are the property names (<c>title</c>, <c>album_artist</c>).
    /// </summary>
    public static JsonElement Merge(JsonElement? caller, FileTags tags)
    {
        ArgumentHelpers.ThrowIfNull(tags);
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer)) {
            writer.WriteStartObject();
            if (caller is { ValueKind: JsonValueKind.Object } element) {
                foreach (var property in element.EnumerateObject()) {
                    if (property.NameEquals(PropertyName))
                        continue;

                    property.WriteTo(writer);
                }
            }

            writer.WritePropertyName(PropertyName);
            writer.WriteStartObject();
            foreach (var pair in tags.Tags)
                writer.WriteString(pair.Key.Id, pair.Value);

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        using var document = JsonDocument.Parse(buffer.ToArray());
        return document.RootElement.Clone();
    }
}
