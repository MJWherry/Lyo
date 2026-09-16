using System.Text.Json;
using Lyo.Desktop;
using Lyo.Exceptions;

namespace Lyo.FileManager;

/// <summary>Persists the server list next to the screenshot settings.</summary>
public static class FileManagerSettingsStore
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

    /// <summary>Reads servers. A missing or unreadable file returns an empty list. An older file that is only the server array still loads.</summary>
    public static IReadOnlyList<FileManagerServer> Load() => LoadDocument(ConfigPath()).Servers;

    /// <summary>Reads servers and the dock layout from <paramref name="path" />.</summary>
    public static FileManagerSettingsDocument LoadDocument(string path)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        try {
            if (!File.Exists(path))
                return new();
            var text = File.ReadAllText(path).TrimStart();
            FileManagerSettingsDocument document;
            if (text.StartsWith('['))
                document = new() { Servers = JsonSerializer.Deserialize<List<FileManagerServer>>(text, Json) ?? [] };
            else
                document = JsonSerializer.Deserialize<FileManagerSettingsDocument>(text, Json) ?? new();
            FileManagerKeyStores.AdoptLegacy(document);
            return document;
        }
        catch (Exception) {
            return new();
        }
    }

    /// <summary>Writes <paramref name="servers" /> and keeps the layout already stored beside them.</summary>
    public static void Save(IReadOnlyList<FileManagerServer> servers)
    {
        ArgumentHelpers.ThrowIfNull(servers);
        var path = ConfigPath();
        var existing = LoadDocument(path);
        Save(path, servers, existing.Layout, existing.Bubble, existing.ShowTreeFiles, existing.KeyStores);
    }

    /// <summary>
    /// Writes <paramref name="servers" />, <paramref name="layout" />, and <paramref name="keyStores" /> to <paramref name="path" />.
    /// A null key-store list keeps the stores already in the file.
    /// </summary>
    public static void Save(
        string path,
        IReadOnlyList<FileManagerServer> servers,
        FileManagerLayout layout,
        FileManagerDockBubble? bubble = null,
        bool showTreeFiles = false,
        IReadOnlyList<FileManagerKeyStore>? keyStores = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        ArgumentHelpers.ThrowIfNull(servers);
        ArgumentHelpers.ThrowIfNull(layout);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        var kept = keyStores ?? (File.Exists(path) ? LoadDocument(path).KeyStores : []);
        var document = new FileManagerSettingsDocument {
            Servers = [..servers],
            Layout = layout,
            Bubble = bubble ?? new(),
            ShowTreeFiles = showTreeFiles,
            KeyStores = [..kept]
        };
        File.WriteAllText(path, JsonSerializer.Serialize(document, Json));
    }

    /// <summary><c>$XDG_CONFIG_HOME/lyo/file-manager.json</c> or <c>%AppData%\Lyo\file-manager.json</c>.</summary>
    public static string ConfigPath() => DesktopConfig.File("file-manager.json");
}
