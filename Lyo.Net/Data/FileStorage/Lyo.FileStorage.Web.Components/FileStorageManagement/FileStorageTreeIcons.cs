using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.Records;
using Lyo.IO.FileSystem;
using Lyo.IO.FileSystem.Web.Components;
using MudBlazor;

namespace Lyo.FileStorage.Web.Components.FileStorageManagement;

/// <summary>MudBlazor icons for tree rows from <see cref="FileTypeInfo" />.</summary>
public static class FileStorageTreeIcons
{
    /// <summary>Folder, or a file-type icon from <c>OriginalFileName</c> / <see cref="FileSystemEntry.Name" />.</summary>
    public static string Resolve(FileTreeNode node)
    {
        if (node.Entry.IsDirectory)
            return Icons.Material.Filled.Folder;

        var name = node.Entry.Name;
        if (node.Entry.Properties != null
            && node.Entry.Properties.TryGetValue("OriginalFileName", out var original)
            && !string.IsNullOrWhiteSpace(original))
            name = original;

        var type = FileTypeInfo.FromFilePath(name);
        if (string.Equals(type.Name, "PDF", StringComparison.OrdinalIgnoreCase))
            return Icons.Material.Filled.PictureAsPdf;

        if (string.Equals(type.Name, "CSV", StringComparison.OrdinalIgnoreCase)
            || string.Equals(type.Name, "XLS", StringComparison.OrdinalIgnoreCase)
            || string.Equals(type.Name, "XLSX", StringComparison.OrdinalIgnoreCase))
            return Icons.Material.Filled.TableChart;

        return type.Category switch {
            FileTypeCategory.Images => Icons.Material.Filled.Image,
            FileTypeCategory.Documents => Icons.Material.Filled.Description,
            FileTypeCategory.DataFiles => Icons.Material.Filled.Code,
            FileTypeCategory.Audio => Icons.Material.Filled.AudioFile,
            FileTypeCategory.Video => Icons.Material.Filled.VideoFile,
            FileTypeCategory.Compressed => Icons.Material.Filled.FolderZip,
            FileTypeCategory.Encrypted => Icons.Material.Filled.Lock,
            FileTypeCategory.PackageManager => Icons.Material.Filled.Inventory2,
            var _ => Icons.Material.Filled.InsertDriveFile
        };
    }
}
