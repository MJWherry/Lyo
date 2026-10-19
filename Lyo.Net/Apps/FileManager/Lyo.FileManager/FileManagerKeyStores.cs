using System.Text.Json;
using Lyo.Exceptions;
using Lyo.KeyStore;

namespace Lyo.FileManager;

/// <summary>Turns a server's old passphrase and AWS fields into a catalog entry on load.</summary>
public static class FileManagerKeyStores
{
    private static readonly JsonSerializerOptions Json = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Reads one local key store from a key file. The file is <c>{ "keys": [ { "keyId", "version", "current", "key", "metadata" } ] }</c>.
    /// <paramref name="fallbackName" /> names the store. <c>key</c> is kept as raw bytes, not derived as a passphrase.
    /// </summary>
    public static IReadOnlyList<FileManagerKeyStore> ReadLocal(string json, string? fallbackName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(json);
        JsonDocument document;
        try {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException) {
            throw new InvalidOperationException("The file is not a local key store.");
        }

        using (document) {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !TryProperty(root, "keys", out var entries) || entries.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("The file is not a local key store.");
            if (entries.GetArrayLength() == 0)
                throw new InvalidOperationException("The file has no local key store.");

            var keys = new List<FileManagerStoredKey>();
            foreach (var entry in entries.EnumerateArray()) {
                if (entry.ValueKind != JsonValueKind.Object || !TryProperty(entry, "keyId", out var keyId) || !TryProperty(entry, "key", out var raw))
                    throw new InvalidOperationException("The file is not a local key store.");
                var id = keyId.ValueKind == JsonValueKind.String ? keyId.GetString() : null;
                var material = raw.ValueKind == JsonValueKind.String ? raw.GetString() : null;
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(material))
                    throw new InvalidOperationException("The file is not a local key store.");
                var version = "1";
                if (TryProperty(entry, "version", out var versionElement) && versionElement.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(versionElement.GetString()))
                    version = versionElement.GetString()!;
                KeyMetadata? metadata = null;
                if (TryProperty(entry, "metadata", out var metadataElement) && metadataElement.ValueKind == JsonValueKind.Object)
                    metadata = JsonSerializer.Deserialize<KeyMetadata>(metadataElement.GetRawText(), Json);
                keys.Add(new() {
                    Id = id.Trim(),
                    Version = version,
                    RawKey = material,
                    Current = TryProperty(entry, "current", out var current) && current.ValueKind == JsonValueKind.True,
                    Metadata = metadata
                });
            }

            var name = string.IsNullOrWhiteSpace(fallbackName) ? "Key store" : fallbackName.Trim();
            return [new FileManagerKeyStore { Name = name, Kind = FileManagerKeyStoreKind.Local, Keys = keys }];
        }
    }

    private static bool TryProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.TryGetProperty(name, out value))
            return true;
        var pascal = char.ToUpperInvariant(name[0]) + name[1..];
        return element.TryGetProperty(pascal, out value);
    }
}
