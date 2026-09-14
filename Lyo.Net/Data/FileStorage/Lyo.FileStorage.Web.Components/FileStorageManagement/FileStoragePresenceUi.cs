using Lyo.Api.FileStorage.Models;
using Lyo.IO.FileSystem;
using MudBlazor;

namespace Lyo.FileStorage.Web.Components.FileStorageManagement;

/// <summary>Tree/inspector helpers for catalog vs physical presence.</summary>
public static class FileStoragePresenceUi
{
    /// <summary>Reads <c>Presence</c> from entry properties.</summary>
    public static FileStoragePresence Read(FileSystemEntry entry)
    {
        if (entry.Properties != null
            && entry.Properties.TryGetValue("Presence", out var text)
            && Enum.TryParse<FileStoragePresence>(text, out var parsed))
            return parsed;

        return FileStoragePresence.None;
    }

    /// <summary>True when the row is in the metadata catalog (not physical-only).</summary>
    public static bool InCatalog(FileStoragePresence presence, Guid? fileId)
        => fileId != null && presence != FileStoragePresence.Physical;

    /// <summary>True when a catalog file id is present on the entry.</summary>
    public static bool TryFileId(FileSystemEntry entry, out Guid fileId)
    {
        fileId = default;
        return entry.Properties != null
               && entry.Properties.TryGetValue("FileId", out var text)
               && Guid.TryParse(text, out fileId)
               && fileId != Guid.Empty;
    }

    /// <summary>Physical object key when the folder list supplied one.</summary>
    public static string? PhysicalKey(FileSystemEntry entry)
        => entry.Properties != null
           && entry.Properties.TryGetValue("PhysicalKey", out var key)
           && !string.IsNullOrWhiteSpace(key)
            ? key
            : null;

    /// <summary>Filled chip for a file row. Directories and unknown presence return false.</summary>
    public static bool TryChip(FileSystemEntry entry, out string label, out string hint, out Color color, out string icon)
    {
        label = string.Empty;
        hint = string.Empty;
        color = Color.Default;
        icon = Icons.Material.Filled.HelpOutline;
        if (entry.IsDirectory)
            return false;

        switch (Read(entry)) {
            case FileStoragePresence.Both:
                label = "Both";
                hint = "In the catalog and on disk";
                color = Color.Success;
                icon = Icons.Material.Filled.CloudDone;
                return true;
            case FileStoragePresence.Store:
                label = "Store only";
                hint = "In the catalog; no physical object";
                color = Color.Error;
                icon = Icons.Material.Filled.CloudOff;
                return true;
            case FileStoragePresence.Physical:
                label = "Physical only";
                hint = "On disk; not in the catalog";
                color = Color.Info;
                icon = Icons.Material.Filled.SdStorage;
                return true;
            default:
                return false;
        }
    }
}
