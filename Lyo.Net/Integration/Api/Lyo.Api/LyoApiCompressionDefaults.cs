using Lyo.Common.Enums;
using Lyo.Common.Records;

namespace Lyo.Api;

/// <summary>
/// MIME types used by <see cref="LyoApiCompressionExtensions.AddLyoApiCompression" />. Compresses every content type except payloads that are already compressed
/// (images other than SVG, audio, archives, Open XML Office documents, PDF).
/// </summary>
public static class LyoApiCompressionDefaults
{
    /// <summary>ASP.NET Core wildcard that matches any response <c>Content-Type</c> not listed in <see cref="ExcludedMimeTypes" />.</summary>
    public static IReadOnlyList<string> MimeTypes { get; } = ["*/*"];

    /// <summary>MIME types skipped by response compression. Never includes <c>application/octet-stream</c> so unknown/bin downloads still compress.</summary>
    public static IReadOnlyList<string> ExcludedMimeTypes { get; } = BuildExcludedMimeTypes();

    private static IReadOnlyList<string> BuildExcludedMimeTypes()
    {
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var type in FileTypeInfo.All) {
            var skip = type.Category is FileTypeCategory.Compressed or FileTypeCategory.Audio or FileTypeCategory.PackageManager
                || (type.Category == FileTypeCategory.Images && type != FileTypeInfo.Svg)
                || type == FileTypeInfo.Pdf
                || type == FileTypeInfo.Docx
                || type == FileTypeInfo.Xlsx;
            if (!skip)
                continue;

            excluded.Add(type.MimeType);
            foreach (var alias in type.MimeTypeAliases)
                excluded.Add(alias);
        }

        excluded.Remove(FileTypeInfo.Unknown.MimeType);
        return [..excluded];
    }
}
