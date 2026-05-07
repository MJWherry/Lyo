using System.Net;
using Lyo.Common.Enums;
using Lyo.Common.Records;

namespace Lyo.Common.Extensions;

/// <summary>
/// Extension methods that map paths, MIME strings, HTTP codes, units, and geographic inputs to Lyo metadata records (<see cref="FileTypeInfo" />,
/// <see cref="HttpStatusCodeInfo" />, <see cref="GeographicInfo" />, and related types).
/// </summary>
public static class TypeInfoExtensions
{
    /// <summary>Gets the FileTypeInfo from a file path or extension.</summary>
    /// <param name="filePath">The file path or extension (with or without dot).</param>
    /// <returns>The FileTypeInfo, or Unknown if not recognized.</returns>
    public static FileTypeInfo GetFileTypeFromExtension(this string? filePath) => FileTypeInfo.FromFilePath(filePath);

    /// <summary>Gets the FileTypeInfo from a FileInfo.</summary>
    /// <param name="fileInfo">The file info.</param>
    /// <returns>The FileTypeInfo, or Unknown if not recognized.</returns>
    public static FileTypeInfo GetFileTypeFromExtension(this FileInfo? fileInfo) => FileTypeInfo.FromFileInfo(fileInfo);

    /// <summary>Gets the FileTypeInfo from a MIME type string.</summary>
    /// <param name="mimeType">The MIME type string (e.g., "application/pdf").</param>
    /// <returns>The FileTypeInfo, or Unknown if not recognized.</returns>
    public static FileTypeInfo GetFileTypeFromMimeValue(this string? mimeType) => FileTypeInfo.FromMimeType(mimeType);

    /// <summary>Gets FileTypeInfo values that match the specified category.</summary>
    /// <param name="category">The file type category (e.g., FileTypeCategory.Documents, FileTypeCategory.Images).</param>
    /// <returns>An enumerable of FileTypeInfo values that match the category.</returns>
    public static IEnumerable<FileTypeInfo> ByCategory(this FileTypeCategory category) => FileTypeInfo.ByCategory(category);

    /// <summary>Finds an HttpStatusCodeInfo by its numeric code.</summary>
    /// <param name="code">The HTTP status code (e.g., 200, 404, 500).</param>
    /// <returns>The corresponding HttpStatusCodeInfo, or Unknown if not found.</returns>
    public static HttpStatusCodeInfo FromHttpStatusCode(this int code) => HttpStatusCodeInfo.FromCode(code);

    /// <summary>Finds an HttpStatusCodeInfo by System.Net.HttpStatusCode enum.</summary>
    /// <param name="httpStatusCode">The System.Net.HttpStatusCode enum value.</param>
    /// <returns>The corresponding HttpStatusCodeInfo, or Unknown if not found.</returns>
    public static HttpStatusCodeInfo ToHttpStatusCodeInfo(this HttpStatusCode httpStatusCode) => HttpStatusCodeInfo.FromHttpStatusCode(httpStatusCode);

    /// <summary>Gets HttpStatusCodeInfo values that match the specified category.</summary>
    /// <param name="category">The HTTP status code category (e.g., HttpStatusCodeCategory.Success, HttpStatusCodeCategory.ClientError).</param>
    /// <returns>An enumerable of HttpStatusCodeInfo values that match the category.</returns>
    public static IEnumerable<HttpStatusCodeInfo> ByCategory(this HttpStatusCodeCategory category) => HttpStatusCodeInfo.ByCategory(category);

    /// <summary>Finds a FileSizeUnitInfo by its abbreviation.</summary>
    /// <param name="abbreviation">The unit abbreviation (e.g., "KB", "MB", "GB").</param>
    /// <returns>The corresponding FileSizeUnitInfo, or Unknown if not found.</returns>
    public static FileSizeUnitInfo FromAbbreviation(this string abbreviation) => FileSizeUnitInfo.FromAbbreviation(abbreviation);

    /// <summary>Converts FileSizeUnit enum to FileSizeUnitInfo.</summary>
    /// <param name="fileSizeUnit">The FileSizeUnit enum value.</param>
    /// <returns>The corresponding FileSizeUnitInfo, or Unknown if not found.</returns>
    public static FileSizeUnitInfo ToFileSizeUnitInfo(this FileSizeUnit fileSizeUnit) => FileSizeUnitInfo.FromFileSizeUnit(fileSizeUnit);

    /// <summary>Finds GeographicInfo by US state.</summary>
    /// <param name="state">The US state.</param>
    /// <returns>The corresponding GeographicInfo, or Unknown if not found.</returns>
    public static GeographicInfo ToGeographicInfo(this USState state) => GeographicInfo.FromState(state);

    /// <summary>Finds GeographicInfo by country code.</summary>
    /// <param name="country">The country code.</param>
    /// <returns>The corresponding GeographicInfo, or Unknown if not found.</returns>
    public static GeographicInfo ToGeographicInfo(this CountryCode country) => GeographicInfo.FromCountry(country);

    /// <summary>Finds GeographicInfo by timezone ID string.</summary>
    /// <param name="timeZoneId">The IANA timezone identifier (e.g., "America/New_York").</param>
    /// <returns>The corresponding GeographicInfo, or Unknown if not found.</returns>
    public static GeographicInfo FromTimeZone(this string timeZoneId) => GeographicInfo.FromTimeZone(timeZoneId);

    /// <summary>Finds GeographicInfo by TimeZoneInfo object.</summary>
    /// <param name="timeZone">The TimeZoneInfo object.</param>
    /// <returns>The corresponding GeographicInfo, or Unknown if not found.</returns>
    public static GeographicInfo ToGeographicInfo(this TimeZoneInfo timeZone) => GeographicInfo.FromTimeZone(timeZone);

    /// <summary>Gets all GeographicInfo values for the specified country.</summary>
    /// <param name="country">The country code.</param>
    /// <returns>An enumerable of GeographicInfo values for the country.</returns>
    public static IEnumerable<GeographicInfo> ByCountry(this CountryCode country) => GeographicInfo.ByCountry(country);

    /// <summary>Gets all GeographicInfo values for the specified state.</summary>
    /// <param name="state">The US state.</param>
    /// <returns>An enumerable of GeographicInfo values for the state.</returns>
    public static IEnumerable<GeographicInfo> ByState(this USState state) => GeographicInfo.ByState(state);

    /// <summary>Gets the audio format from a file extension.</summary>
    /// <param name="filePath">The file path or extension (with or without dot).</param>
    /// <returns>The audio format, or Unknown if not recognized.</returns>
    public static AudioFormat GetAudioFormatFromExtension(this string? filePath)
    {
        if (filePath.IsNullOrWhitespace())
            return AudioFormat.Unknown;

        var extension = Path.GetExtension(filePath).TrimStart('.');
        if (extension.IsNullOrWhitespace())
            extension = filePath.TrimStart('.');

        return extension.ToLowerInvariant() switch {
            "wav" => AudioFormat.Wav,
            "mp3" => AudioFormat.Mp3,
            "ogg" => AudioFormat.Ogg,
            "flac" => AudioFormat.Flac,
            "aac" => AudioFormat.Aac,
            "m4a" => AudioFormat.M4a,
            "opus" => AudioFormat.Opus,
            "pcm" => AudioFormat.Pcm,
            "webm" => AudioFormat.Webm,
            var _ => AudioFormat.Unknown
        };
    }

    /// <summary>Gets the MIME type from a file extension.</summary>
    /// <param name="filePath">The file path or extension (with or without dot).</param>
    /// <returns>The MIME type, or Unknown if not recognized.</returns>
    public static MimeType GetMimeTypeFromExtension(this string? filePath)
    {
        if (filePath.IsNullOrWhitespace())
            return MimeType.Unknown;

        var extension = Path.GetExtension(filePath).TrimStart('.');
        if (extension.IsNullOrWhitespace())
            extension = filePath.TrimStart('.');

        return Enum.TryParse<MimeType>(extension, out var result) ? result : MimeType.Unknown;
    }

    /// <summary>Gets the MIME type from a FileInfo.</summary>
    /// <param name="fileInfo">The file info.</param>
    /// <returns>The MIME type, or Unknown if not recognized.</returns>
    public static MimeType GetMimeTypeFromExtension(this FileInfo? fileInfo) => fileInfo == null ? MimeType.Unknown : fileInfo.FullName.GetMimeTypeFromExtension();
}