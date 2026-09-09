using System.Net;
using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Extensions;
using Lyo.Common.Metadata.Records;

namespace Lyo.Common.Metadata.Extensions;

/// <summary>
/// Helpers that resolve paths, MIME strings, HTTP codes, units, and geographic tokens into Lyo metadata records (<see cref="FileTypeInfo" />,
/// <see cref="MimeTypeInfo" />, <see cref="HttpStatusCodeInfo" />, <see cref="GeographicInfo" />, and related types).
/// </summary>
public static class TypeInfoExtensions
{
    /// <summary>Resolves <see cref="FileTypeInfo" /> from a path or extension token.</summary>
    /// <param name="filePath">Path or extension, with or without a leading dot.</param>
    /// <returns>The matching <see cref="FileTypeInfo" />, or <see cref="FileTypeInfo.Unknown" /> when unrecognized.</returns>
    public static FileTypeInfo GetFileTypeFromExtension(this string? filePath) => FileTypeInfo.FromFilePath(filePath);

    /// <summary>Resolves <see cref="FileTypeInfo" /> from a <see cref="FileInfo" />.</summary>
    /// <param name="fileInfo">The file metadata.</param>
    /// <returns>The matching <see cref="FileTypeInfo" />, or <see cref="FileTypeInfo.Unknown" /> when unrecognized.</returns>
    public static FileTypeInfo GetFileTypeFromExtension(this FileInfo? fileInfo) => FileTypeInfo.FromFileInfo(fileInfo);

    /// <summary>Resolves <see cref="FileTypeInfo" /> from a MIME string.</summary>
    /// <param name="mimeType">MIME token, for example <c>application/pdf</c>.</param>
    /// <returns>The matching <see cref="FileTypeInfo" />, or <see cref="FileTypeInfo.Unknown" /> when unrecognized.</returns>
    public static FileTypeInfo GetFileTypeFromMimeValue(this string? mimeType) => FileTypeInfo.FromMimeType(mimeType);

    /// <summary>Lists <see cref="FileTypeInfo" /> entries that belong to <paramref name="category" />.</summary>
    /// <param name="category">Bucket such as <see cref="FileTypeCategory.Documents" /> or <see cref="FileTypeCategory.Images" />.</param>
    /// <returns>Matching catalog rows for that category.</returns>
    public static IEnumerable<FileTypeInfo> ByCategory(this FileTypeCategory category) => FileTypeInfo.ByCategory(category);

    /// <summary>Looks up <see cref="HttpStatusCodeInfo" /> by numeric status.</summary>
    /// <param name="code">HTTP code, for example 200, 404, or 500.</param>
    /// <returns>The catalog row, or <see cref="HttpStatusCodeInfo.Unknown" /> when the code is not registered.</returns>
    public static HttpStatusCodeInfo FromHttpStatusCode(this int code) => HttpStatusCodeInfo.FromCode(code);

    /// <summary>Maps a <see cref="HttpStatusCode" /> enum value to <see cref="HttpStatusCodeInfo" />.</summary>
    /// <param name="httpStatusCode">The <see cref="HttpStatusCode" /> member.</param>
    /// <returns>The catalog row, or <see cref="HttpStatusCodeInfo.Unknown" /> when the code is not registered.</returns>
    public static HttpStatusCodeInfo ToHttpStatusCodeInfo(this HttpStatusCode httpStatusCode) => HttpStatusCodeInfo.FromHttpStatusCode(httpStatusCode);

    /// <summary>Lists <see cref="HttpStatusCodeInfo" /> rows in <paramref name="category" />.</summary>
    /// <param name="category">Bucket such as <see cref="HttpStatusCodeCategory.Success" /> or <see cref="HttpStatusCodeCategory.ClientError" />.</param>
    /// <returns>Matching catalog rows for that category.</returns>
    public static IEnumerable<HttpStatusCodeInfo> ByCategory(this HttpStatusCodeCategory category) => HttpStatusCodeInfo.ByCategory(category);

    /// <summary>Looks up <see cref="FileSizeUnitInfo" /> by abbreviation.</summary>
    /// <param name="abbreviation">Unit token, for example <c>KB</c>, <c>MB</c>, or <c>GB</c>.</param>
    /// <returns>The catalog row, or <see cref="FileSizeUnitInfo.Unknown" /> when the token is not registered.</returns>
    public static FileSizeUnitInfo FromAbbreviation(this string abbreviation) => FileSizeUnitInfo.FromAbbreviation(abbreviation);

    /// <summary>Maps a <see cref="FileSizeUnit" /> member to <see cref="FileSizeUnitInfo" />.</summary>
    /// <param name="fileSizeUnit">The <see cref="FileSizeUnit" /> value.</param>
    /// <returns>The catalog row, or <see cref="FileSizeUnitInfo.Unknown" /> when the unit is not registered.</returns>
    public static FileSizeUnitInfo ToFileSizeUnitInfo(this FileSizeUnit fileSizeUnit) => FileSizeUnitInfo.FromFileSizeUnit(fileSizeUnit);

    /// <summary>Looks up <see cref="GeographicInfo" /> from a <see cref="USState" />.</summary>
    /// <param name="state">The US state.</param>
    /// <returns>The catalog row, or <see cref="GeographicInfo.Unknown" /> when the state is not registered.</returns>
    public static GeographicInfo ToGeographicInfo(this USState state) => GeographicInfo.FromState(state);

    /// <summary>Looks up <see cref="GeographicInfo" /> from a <see cref="CountryCode" />.</summary>
    /// <param name="country">The country code.</param>
    /// <returns>The catalog row, or <see cref="GeographicInfo.Unknown" /> when the country is not registered.</returns>
    public static GeographicInfo ToGeographicInfo(this CountryCode country) => GeographicInfo.FromCountry(country);

    /// <summary>Looks up <see cref="GeographicInfo" /> from an IANA timezone id.</summary>
    /// <param name="timeZoneId">IANA identifier, for example <c>America/New_York</c>.</param>
    /// <returns>The catalog row, or <see cref="GeographicInfo.Unknown" /> when the id is not registered.</returns>
    public static GeographicInfo FromTimeZone(this string timeZoneId) => GeographicInfo.FromTimeZone(timeZoneId);

    /// <summary>Looks up <see cref="GeographicInfo" /> from a <see cref="TimeZoneInfo" />.</summary>
    /// <param name="timeZone">The <see cref="TimeZoneInfo" /> instance.</param>
    /// <returns>The catalog row, or <see cref="GeographicInfo.Unknown" /> when the zone is not registered.</returns>
    public static GeographicInfo ToGeographicInfo(this TimeZoneInfo timeZone) => GeographicInfo.FromTimeZone(timeZone);

    /// <summary>Lists every <see cref="GeographicInfo" /> row for <paramref name="country" />.</summary>
    /// <param name="country">The country code.</param>
    /// <returns>Catalog rows for that country.</returns>
    public static IEnumerable<GeographicInfo> ByCountry(this CountryCode country) => GeographicInfo.ByCountry(country);

    /// <summary>Lists every <see cref="GeographicInfo" /> row for <paramref name="state" />.</summary>
    /// <param name="state">The US state.</param>
    /// <returns>Catalog rows for that state.</returns>
    public static IEnumerable<GeographicInfo> ByState(this USState state) => GeographicInfo.ByState(state);

    /// <summary>Maps a path or extension token to <see cref="AudioFormat" />.</summary>
    /// <param name="filePath">Path or extension, with or without a leading dot.</param>
    /// <returns>The matching format, or <see cref="AudioFormat.Unknown" /> when unrecognized.</returns>
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

    /// <summary>Maps a path or extension token to <see cref="MimeTypeInfo" /> via <see cref="FileTypeInfo.FromFilePath" />.</summary>
    /// <param name="filePath">Path or extension, with or without a leading dot.</param>
    /// <returns>The matching MIME, or <see cref="MimeTypeInfo.Unknown" /> when unrecognized.</returns>
    public static MimeTypeInfo GetMimeTypeFromExtension(this string? filePath) => FileTypeInfo.FromFilePath(filePath).Mime;

    /// <summary>Maps a <see cref="FileInfo" /> to <see cref="MimeTypeInfo" />.</summary>
    /// <param name="fileInfo">The file metadata.</param>
    /// <returns>The matching MIME, or <see cref="MimeTypeInfo.Unknown" /> when unrecognized.</returns>
    public static MimeTypeInfo GetMimeTypeFromExtension(this FileInfo? fileInfo) => fileInfo == null ? MimeTypeInfo.Unknown : fileInfo.FullName.GetMimeTypeFromExtension();
}