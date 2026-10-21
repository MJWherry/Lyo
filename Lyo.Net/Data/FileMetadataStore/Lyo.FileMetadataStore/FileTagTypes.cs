using System.Globalization;
using Lyo.Exceptions;
using Lyo.FileMetadataStore.Models;

namespace Lyo.FileMetadataStore;

/// <summary>Built-in file tag types and the rules for type and value text.</summary>
public static class FileTagTypes
{
    /// <summary>Extension of the display name, without the dot.</summary>
    public const string FileType = "file-type";

    /// <summary>UTC timestamp of the first stored commit. Grouped by year, month, and day.</summary>
    public const string Imported = "imported";

    /// <summary>Maximum length of <see cref="FileTag.TagType" />.</summary>
    public const int MaxTagTypeLength = 50;

    /// <summary>Maximum length of <see cref="FileTag.Value" />.</summary>
    public const int MaxValueLength = 200;

    /// <summary>Maximum length of <see cref="FileTag.Slug" />.</summary>
    public const int MaxSlugLength = 200;

    /// <summary>True for types the service writes and callers cannot add or remove.</summary>
    public static bool IsReserved(string tagType) => string.Equals(tagType, FileType, StringComparison.Ordinal) || string.Equals(tagType, Imported, StringComparison.Ordinal);

    /// <summary>Rejects empty text, overlong text, and slash characters. Tag types also cannot contain a colon.</summary>
    public static void Validate(string tagType, string value, string? slug)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tagType);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value);
        if (tagType.Length > MaxTagTypeLength || value.Length > MaxValueLength || (slug?.Length ?? 0) > MaxSlugLength)
            throw new ArgumentException("Tag type, value, or slug exceeds its maximum length.");

        if (tagType.IndexOf('/') >= 0 || tagType.IndexOf('\\') >= 0 || tagType.IndexOf(':') >= 0 || value.IndexOf('/') >= 0 || value.IndexOf('\\') >= 0)
            throw new ArgumentException("Tag type and value cannot contain a slash, and a tag type cannot contain a colon.");
    }

    /// <summary>Extension of <paramref name="originalFileName" />, lowercased, without the dot. <c>unknown</c> when there is none.</summary>
    public static string FileTypeValue(string? originalFileName)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
            return "unknown";

        var extension = Path.GetExtension(originalFileName);
        if (string.IsNullOrEmpty(extension))
            return "unknown";

        return extension.TrimStart('.').ToLowerInvariant();
    }

    /// <summary>UTC round-trip form of <paramref name="timestamp" />. Unspecified kind is treated as UTC.</summary>
    public static string FormatImported(DateTime timestamp)
    {
        var utc = timestamp.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(timestamp, DateTimeKind.Utc) : timestamp.ToUniversalTime();
        return utc.ToString("O", CultureInfo.InvariantCulture);
    }

    /// <summary>Parses an <c>imported</c> value. Returns false when the text is not a timestamp.</summary>
    public static bool TryParseImported(string value, out DateTime utc)
    {
        if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)) {
            utc = default;
            return false;
        }

        utc = parsed.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc) : parsed.ToUniversalTime();
        return true;
    }
}