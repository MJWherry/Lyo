using Lyo.Common.Metadata.Records;
using Lyo.FileStorage.Web.Components.FileStorageManagement;
using Lyo.Web.Components;
using Lyo.Web.Primitives;
using Lyo.Web.Components.DataGrid;
using MudBlazor;

namespace Lyo.FileStorage.Web.Components;

/// <summary>MudBlazor chip colors for file-type, encryption, and compression cells.</summary>
public static class FileStorageColorHelper
{
    /// <summary>File-type chip from MIME and original filename on a projected row.</summary>
    public static LyoChipSpec FileTypeChip(object? item)
    {
        var mime = MediaTypeOnly(ProjectedValueHelper.GetDisplayValue(item, "ContentType"));
        var type = ResolveFileType(item);
        if (type == FileTypeInfo.Unknown)
            return LyoChips.Of(string.IsNullOrWhiteSpace(mime) ? "File" : mime);

        return LyoChips.Of(type.Name, HueForType(type));
    }

    /// <summary>Known <see cref="FileTypeInfo" /> from MIME, then filename, used for preview routing.</summary>
    public static FileTypeInfo ResolveFileType(object? item)
    {
        var mime = MediaTypeOnly(ProjectedValueHelper.GetDisplayValue(item, "ContentType"));
        var name = FileStorageGridRowHelper.GetOriginalFileNameFromRow(item);
        if (string.IsNullOrWhiteSpace(name))
            name = FileStorageGridRowHelper.GetSourceFileNameFromRow(item);
        var type = FileTypeInfo.FromMimeType(mime);
        return type == FileTypeInfo.Unknown ? FileTypeInfo.FromFilePath(name) : type;
    }

    /// <summary>DEK algorithm chip; blank shows <c>None</c>.</summary>
    public static LyoChipSpec EncryptionChip(object? item)
        => EncryptionChip(ProjectedValueHelper.GetDisplayValue(item, "DataEncryptionKeyAlgorithm"));

    /// <summary>Chip for a persisted encryption algorithm name.</summary>
    public static LyoChipSpec EncryptionChip(string? algorithm)
    {
        if (string.IsNullOrWhiteSpace(algorithm))
            return LyoChips.Of("None", Color.Default, Icons.Material.Filled.LockOpen);

        var hue = Normalize(algorithm) switch {
            "AESGCM" or "AESGCMRSA" or "AESCCM" or "AESSIV" => LyoChipHue.Blue,
            "CHACHA20POLY1305" or "XCHACHA20POLY1305" => LyoChipHue.Teal,
            "RSA" => LyoChipHue.Violet,
            var _ => null
        };
        return hue is null
            ? LyoChips.Of(algorithm, Color.Default, Icons.Material.Filled.Lock)
            : LyoChips.Of(algorithm, hue, Icons.Material.Filled.Lock);
    }

    /// <summary>Compression algorithm chip; blank shows <c>None</c>.</summary>
    public static LyoChipSpec CompressionChip(object? item)
        => CompressionChip(ProjectedValueHelper.GetDisplayValue(item, "CompressionAlgorithm"));

    /// <summary>Chip for a persisted compression algorithm name.</summary>
    public static LyoChipSpec CompressionChip(string? algorithm)
    {
        if (string.IsNullOrWhiteSpace(algorithm))
            return LyoChips.Of("None", Color.Default);

        var hue = Normalize(algorithm) switch {
            "BROTLI" => LyoChipHue.Lime,
            "GZIP" => LyoChipHue.Amber,
            "DEFLATE" or "ZLIB" => LyoChipHue.Cyan,
            "LZ4" => LyoChipHue.Coral,
            var _ => null
        };
        return hue is null
            ? LyoChips.Of(algorithm, icon: Icons.Material.Filled.Compress)
            : LyoChips.Of(algorithm, hue, Icons.Material.Filled.Compress);
    }

    /// <summary>
    /// Hue for one file type name. A stride of five around <see cref="LyoChipHue.All" /> keeps names whose
    /// character sums differ by one (CSV and HTML) about 150° apart, so they do not both read as cyan.
    /// </summary>
    private static LyoChipHue HueForType(FileTypeInfo type)
    {
        var sum = 0;
        foreach (var c in type.Name)
            sum += c;
        return LyoChipHue.All[sum * 5 % LyoChipHue.All.Count];
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
