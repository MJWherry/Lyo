using Lyo.Api.FileStorage.Models;
using Lyo.IO.FileSystem;
using MudBlazor;

namespace Lyo.FileStorage.Web.Components.FileStorageManagement;

/// <summary>Tree/inspector helpers for catalog vs backend presence.</summary>
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

    /// <summary>True when the row is in the metadata catalog (not backend-only).</summary>
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

    /// <summary>Short label for the storage backend.</summary>
    public static string DisplayName(FileStorageBackendKind kind)
        => kind switch {
            FileStorageBackendKind.Local => "Local",
            FileStorageBackendKind.AwsS3 => "S3",
            FileStorageBackendKind.AzureBlob => "Azure Blob",
            FileStorageBackendKind.Other => "object store",
            var _ => "storage"
        };

    /// <summary>Filled chip for a file row. Directories and unknown presence return false.</summary>
    public static bool TryChip(
        FileSystemEntry entry,
        FileStorageBackendKind backendKind,
        out string label,
        out string hint,
        out Color color,
        out string icon)
    {
        label = string.Empty;
        hint = string.Empty;
        color = Color.Default;
        icon = Icons.Material.Filled.HelpOutline;
        if (entry.IsDirectory)
            return false;

        var kind = DisplayName(backendKind);
        switch (Read(entry)) {
            case FileStoragePresence.Both:
                label = "Both";
                hint = $"In the catalog and on {kind}";
                color = Color.Success;
                icon = Icons.Material.Filled.CloudDone;
                return true;
            case FileStoragePresence.Store:
                label = "Catalog only";
                hint = $"In the catalog; no object on {kind}";
                color = Color.Error;
                icon = Icons.Material.Filled.CloudOff;
                return true;
            case FileStoragePresence.Physical:
                label = $"{kind} only";
                hint = $"On {kind}; not in the catalog";
                color = Color.Info;
                icon = Icons.Material.Filled.SdStorage;
                return true;
            default:
                return false;
        }
    }

    /// <summary>Walks loaded descendants into catalog ids and backend-only keys.</summary>
    public static void Collect(FileTreeNode node, HashSet<Guid> catalogIds, HashSet<string> physicalKeys)
    {
        if (node.Entry.IsDirectory) {
            foreach (var child in node.Children)
                Collect(child, catalogIds, physicalKeys);

            return;
        }

        var presence = Read(node.Entry);
        Guid? fileId = TryFileId(node.Entry, out var parsed) ? parsed : null;
        if (InCatalog(presence, fileId) && fileId is { } catalogId) {
            catalogIds.Add(catalogId);
            return;
        }

        var key = PhysicalKey(node.Entry);
        if (presence == FileStoragePresence.Physical && !string.IsNullOrEmpty(key))
            physicalKeys.Add(key);
    }
}
