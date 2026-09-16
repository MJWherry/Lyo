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

    /// <summary>
    /// Gives each server that still has a passphrase, saved keys, or an AWS key store, and no <see cref="FileManagerServer.KeyStoreId" />,
    /// one catalog entry named after the server. An AWS server copies only the AWS fields. A local server copies the passphrase and keys.
    /// </summary>
    public static void AdoptLegacy(FileManagerSettingsDocument document)
    {
        ArgumentHelpers.ThrowIfNull(document);
        document.Servers ??= [];
        document.KeyStores ??= [];
        foreach (var server in document.Servers) {
            if (server.KeyStoreId != null)
                continue;
            if (server.KeyStoreKind == FileManagerKeyStoreKind.Aws) {
                var aws = new FileManagerKeyStore {
                    Name = NameOf(server),
                    Kind = FileManagerKeyStoreKind.Aws,
                    AwsRegion = server.AwsRegion,
                    AwsSecretPrefix = server.AwsSecretPrefix,
                    AwsAccessKeyId = server.AwsAccessKeyId,
                    AwsSecretAccessKey = server.AwsSecretAccessKey,
                    AwsProfile = server.AwsProfile
                };
                document.KeyStores.Add(aws);
                server.KeyStoreId = aws.Id;
                continue;
            }

            if (!HasLocalMaterial(server))
                continue;
            var local = new FileManagerKeyStore {
                Name = NameOf(server),
                Kind = FileManagerKeyStoreKind.Local,
                Passphrase = string.IsNullOrWhiteSpace(server.Passphrase) ? null : server.Passphrase,
                Keys = CopyKeys(server.Keys)
            };
            document.KeyStores.Add(local);
            server.KeyStoreId = local.Id;
        }
    }

    private static bool HasLocalMaterial(FileManagerServer server)
    {
        if (!string.IsNullOrWhiteSpace(server.Passphrase))
            return true;
        return server.Keys?.Any(key => !string.IsNullOrWhiteSpace(key.Id) && (!string.IsNullOrWhiteSpace(key.Secret) || !string.IsNullOrWhiteSpace(key.RawKey))) == true;
    }

    private static List<FileManagerStoredKey> CopyKeys(List<FileManagerStoredKey>? keys)
        => keys?.Select(CopyKey).ToList() ?? [];

    private static FileManagerStoredKey CopyKey(FileManagerStoredKey key)
        => new() { Id = key.Id, Version = key.Version, Secret = key.Secret, RawKey = key.RawKey, Current = key.Current, Metadata = key.Metadata };

    private static string NameOf(FileManagerServer server)
        => string.IsNullOrWhiteSpace(server.Name) ? "Key store" : server.Name.Trim();

    private static bool TryProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.TryGetProperty(name, out value))
            return true;
        var pascal = char.ToUpperInvariant(name[0]) + name[1..];
        return element.TryGetProperty(pascal, out value);
    }
}
