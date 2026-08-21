using Lyo.Common.Enums;
using Lyo.Common.Records;
using Lyo.FileStorage.Web.Components.FileStorageWorkbench;
using Lyo.Web.Components;
using Lyo.Web.Components.DataGrid;
using MudBlazor;

namespace Lyo.FileStorage.Web.Components;

/// <summary>MudBlazor chip colors for file type, encryption algorithm, and compression algorithm cells.</summary>
public static class FileStorageColorHelper
{
    /// <summary>Color-coded file-type chip from MIME and original filename on a projected row.</summary>
    public static LyoChipSpec FileTypeChip(object? item)
    {
        var mime = MediaTypeOnly(ProjectedValueHelper.GetDisplayValue(item, "ContentType"));
        var type = ResolveFileType(item);
        if (type == FileTypeInfo.Unknown)
            return LyoChips.Of(string.IsNullOrWhiteSpace(mime) ? "File" : mime);

        var color = type.Category switch {
            FileTypeCategory.Images => Color.Success,
            FileTypeCategory.Documents => Color.Info,
            FileTypeCategory.DataFiles => Color.Primary,
            var _ => Color.Default
        };
        return LyoChips.Of(type.Name, color);
    }

    /// <summary>Resolves a known <see cref="FileTypeInfo" /> from MIME, then filename, for preview routing.</summary>
    public static FileTypeInfo ResolveFileType(object? item)
    {
        var mime = MediaTypeOnly(ProjectedValueHelper.GetDisplayValue(item, "ContentType"));
        var name = FileStorageGridRowHelper.GetOriginalFileNameFromRow(item);
        if (string.IsNullOrWhiteSpace(name))
            name = FileStorageGridRowHelper.GetSourceFileNameFromRow(item);
        var type = FileTypeInfo.FromMimeType(mime);
        return type == FileTypeInfo.Unknown ? FileTypeInfo.FromFilePath(name) : type;
    }

    /// <summary>Chip for DEK algorithm; blank is <c>None</c>.</summary>
    public static LyoChipSpec EncryptionChip(object? item)
        => EncryptionChip(ProjectedValueHelper.GetDisplayValue(item, "DataEncryptionKeyAlgorithm"));

    /// <summary>Chip for a stored encryption algorithm name.</summary>
    public static LyoChipSpec EncryptionChip(string? algorithm)
    {
        if (string.IsNullOrWhiteSpace(algorithm))
            return LyoChips.Of("None", Color.Default, Icons.Material.Filled.LockOpen);

        var color = Normalize(algorithm) switch {
            "AESGCM" or "AESGCMRSA" or "AESCCM" or "AESSIV" => Color.Primary,
            "CHACHA20POLY1305" or "XCHACHA20POLY1305" => Color.Tertiary,
            "RSA" => Color.Info,
            var _ => Color.Default
        };
        return LyoChips.Of(algorithm, color, Icons.Material.Filled.Lock);
    }

    /// <summary>Chip for compression algorithm; blank is <c>None</c>.</summary>
    public static LyoChipSpec CompressionChip(object? item)
        => CompressionChip(ProjectedValueHelper.GetDisplayValue(item, "CompressionAlgorithm"));

    /// <summary>Chip for a stored compression algorithm name.</summary>
    public static LyoChipSpec CompressionChip(string? algorithm)
    {
        if (string.IsNullOrWhiteSpace(algorithm))
            return LyoChips.Of("None", Color.Default);

        var color = Normalize(algorithm) switch {
            "BROTLI" => Color.Primary,
            "GZIP" => Color.Info,
            "DEFLATE" or "ZLIB" => Color.Secondary,
            "LZ4" => Color.Tertiary,
            var _ => Color.Default
        };
        return LyoChips.Of(algorithm, color, Icons.Material.Filled.Compress);
    }

    private static string? MediaTypeOnly(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return contentType;

        var semi = contentType.IndexOf(';');
        return semi < 0 ? contentType.Trim() : contentType[..semi].Trim();
    }

    private static string Normalize(string value) => value.Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal).ToUpperInvariant();
}
