using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Lyo.Common.Attributes;
using Lyo.Common.Enums;
using Lyo.Common.Records;

#if NETSTANDARD2_0
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#endif

namespace Lyo.Common;

public static class Extensions
{
    /// <summary>Returns the default value if the string is null or empty.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNull]
    public static string OrDefault(this string? value, [NotNull] string defaultValue) => string.IsNullOrEmpty(value) ? defaultValue : value!;

    /// <summary>Returns the default value if the string is null, empty, or consists only of whitespace.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNull]
    public static string OrDefaultIfWhiteSpace(this string? value, [NotNull] string defaultValue) => string.IsNullOrWhiteSpace(value) ? defaultValue : value!;

    public static string Truncated(this string s, in int? start = 4, in int? end = null, in int ellipsesLength = 3)
    {
        if (string.IsNullOrEmpty(s))
            return string.Empty;

        var ellipses = ".".Repeat(Math.Max(0, ellipsesLength));
        var startLen = start.HasValue ? Math.Min(start.Value, s.Length) : 0;
        var startPart = startLen > 0 ? s.Substring(0, startLen) : string.Empty;
        if (!end.HasValue || end.Value >= s.Length)
            return startLen >= s.Length ? s : $"{startPart}{ellipses}";

        var endIdx = Math.Max(0, Math.Min(end.Value, s.Length - 1));
        if (endIdx <= startLen)
            return $"{startPart}{ellipses}";

        var endPart = s.Substring(endIdx);
        return $"{startPart}{ellipses}{endPart}";
    }

    public static string Truncated(this in Guid guid, int? start = 4, in int? end = null, in int ellipsesLength = 3) => guid.ToString().Truncated(start, end, ellipsesLength);

    public static string Truncated(this in Guid? guid, in int? start = 4, in int? end = null, in int ellipsesLength = 3)
        => (guid ?? Guid.Empty).Truncated(start, end, ellipsesLength);

    public static bool In(this string value, in IEnumerable<string> values, StringComparison comparison = StringComparison.CurrentCulture)
        => values.Any(v => v.Equals(value, comparison));

    public static T? ToScalar<T>(this string? value, string? format = null, IFormatProvider? formatProvider = null)
    {
        if (value is null)
            return default;

        if (typeof(T) == typeof(string))
            return (T?)(object?)value;

        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        formatProvider ??= CultureInfo.InvariantCulture;
        try {
            object? parsed;
            if (targetType == typeof(int))
                parsed = int.TryParse(value, NumberStyles.Any, formatProvider, out var i) ? i : null;
            else if (targetType == typeof(long))
                parsed = long.TryParse(value, NumberStyles.Any, formatProvider, out var l) ? l : null;
            else if (targetType == typeof(double))
                parsed = double.TryParse(value, NumberStyles.Any, formatProvider, out var d) ? d : null;
            else if (targetType == typeof(float))
                parsed = float.TryParse(value, NumberStyles.Any, formatProvider, out var f) ? f : null;
            else if (targetType == typeof(decimal))
                parsed = decimal.TryParse(value, NumberStyles.Any, formatProvider, out var m) ? m : null;
            else if (targetType == typeof(bool))
                parsed = bool.TryParse(value, out var b) ? b : null;
            else if (targetType == typeof(Guid))
                parsed = Guid.TryParse(value, out var g) ? g : null;
            else if (targetType == typeof(DateTime))
                parsed = DateTime.TryParse(value, formatProvider, DateTimeStyles.None, out var dt) ? dt : null;
            else if (targetType == typeof(TimeSpan))
                parsed = TimeSpan.TryParse(value, formatProvider, out var ts) ? ts : null;
            else if (targetType.IsEnum) {
                try {
                    parsed = Enum.Parse(targetType, value);
                }
                catch {
                    parsed = null;
                }
            }
            else
                parsed = Convert.ChangeType(value, targetType, formatProvider);

            if (parsed == null || format == null || parsed is not IFormattable formattable)
                return (T?)parsed;

            var formatted = formattable.ToString(format, formatProvider);
            parsed = Convert.ChangeType(formatted, targetType, formatProvider);
            return (T?)parsed;
        }
        catch {
            return default;
        }
    }

    public static T? GetValueAs<T>(this IDictionary<string, object> source, string key, string? format = null, IFormatProvider? formatProvider = null)
    {
        if (!source.TryGetValue(key, out var v))
            return default;

        formatProvider ??= CultureInfo.InvariantCulture;
        var value = v.ToString();
        return value.ToScalar<T>(format, formatProvider);
    }

    public static T? GetValueAs<T>(this IReadOnlyDictionary<string, object> source, string key, string? format = null, IFormatProvider? formatProvider = null)
    {
        if (!source.TryGetValue(key, out var v) || v is null)
            return default;

        formatProvider ??= CultureInfo.InvariantCulture;
        var value = v.ToString();
        return value.ToScalar<T>(format, formatProvider);
    }

    public static string Repeat(this string value, int amount)
    {
        if (amount <= 0)
            return string.Empty;

        if (amount == 1)
            return value;

        // Single character optimization
        if (value.Length == 1)
            return new(value[0], amount);

#if NET6_0_OR_GREATER
        // Use string.Create for .NET 6+
        return string.Create(
            value.Length * amount, (value, amount), (span, state) => {
                var (str, count) = state;
                for (var i = 0; i < count; i++)
                    str.AsSpan().CopyTo(span.Slice(i * str.Length));
            });
#else
        // Use StringBuilder for .NET Standard 2.0 and earlier
        var sb = new StringBuilder(value.Length * amount);
        for (var i = 0; i < amount; i++)
            sb.Append(value);

        return sb.ToString();
#endif
    }

    public static string? GetDescription<T>(this T? value)
        where T : Enum
    {
        if (value is null)
            return null;

        var field = value.GetType().GetField(value.ToString());
        var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? value.ToString();
    }

    /// <summary>Gets the string value attribute for an enum value, or the enum name if no string value is found.</summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="value">The enum value.</param>
    /// <returns>The string value from the StringValue attribute, or the enum name if not found.</returns>
    public static string? GetStringValue<T>(this T? value)
        where T : Enum
    {
        if (value is null)
            return null;

        var field = value.GetType().GetField(value.ToString());
        var attribute = field?.GetCustomAttribute<StringValueAttribute>();
        return attribute?.Value ?? value.ToString();
    }

    /// <summary>Gets the string value attribute for an enum value, or the enum name if no string value is found.</summary>
    /// <param name="value">The enum value.</param>
    /// <returns>The string value from the StringValue attribute, or the enum name if not found.</returns>
    public static string GetStringValue(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = field?.GetCustomAttribute<StringValueAttribute>();
        return attribute?.Value ?? value.ToString();
    }

    // LanguageCodeInfo extension methods (using new record-based approach)

    /// <summary>Finds a LanguageCodeInfo by its ISO 639-1 (2-letter) code.</summary>
    /// <param name="iso639_1Code">The ISO 639-1 code (e.g., "en", "es", "fr").</param>
    /// <returns>The first matching LanguageCodeInfo (base variant), or Unknown if not found.</returns>
    /// <remarks>When multiple language codes share the same ISO 639-1 code, returns the first one (base variant).</remarks>
    public static LanguageCodeInfo FromISO639_1(this string iso639_1Code) => LanguageCodeInfo.FromIso6391(iso639_1Code);

    /// <summary>Finds a LanguageCodeInfo by its ISO 639-3 (3-letter) code.</summary>
    /// <param name="iso639_3Code">The ISO 639-3 code (e.g., "eng", "spa", "fra").</param>
    /// <returns>The first matching LanguageCodeInfo (base variant), or Unknown if not found.</returns>
    /// <remarks>When multiple language codes share the same ISO 639-3 code, returns the first one (base variant).</remarks>
    public static LanguageCodeInfo FromISO639_3(this string iso639_3Code) => LanguageCodeInfo.FromIso6393(iso639_3Code);

    /// <summary>Finds a LanguageCodeInfo by its BCP 47 code.</summary>
    /// <param name="bcp47Code">The BCP 47 code (e.g., "en-US", "fr-FR").</param>
    /// <returns>The corresponding LanguageCodeInfo, or Unknown if not found.</returns>
    public static LanguageCodeInfo FromBCP_47(this string bcp47Code) => LanguageCodeInfo.FromBcp47(bcp47Code);

    // FileTypeInfo extension methods (using new record-based approach)

    /// <summary>Finds a FileTypeInfo by its MIME type value.</summary>
    /// <param name="mimeValue">The MIME type value (e.g., "application/pdf", "image/jpeg").</param>
    /// <returns>The corresponding FileTypeInfo, or Unknown if not found.</returns>
    public static FileTypeInfo FromMimeValue(this string mimeValue) => FileTypeInfo.FromMimeType(mimeValue);

    /// <summary>Finds a FileTypeInfo by its file extension.</summary>
    /// <param name="extension">The file extension (with or without leading dot, e.g., ".pdf", "pdf", ".jpg", "jpg").</param>
    /// <returns>The corresponding FileTypeInfo, or Unknown if not found.</returns>
    public static FileTypeInfo FromExtension(this string extension) => FileTypeInfo.FromExtension(extension);

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

    // HttpStatusCodeInfo extension methods

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

    // FileSizeUnitInfo extension methods

    /// <summary>Finds a FileSizeUnitInfo by its abbreviation.</summary>
    /// <param name="abbreviation">The unit abbreviation (e.g., "KB", "MB", "GB").</param>
    /// <returns>The corresponding FileSizeUnitInfo, or Unknown if not found.</returns>
    public static FileSizeUnitInfo FromAbbreviation(this string abbreviation) => FileSizeUnitInfo.FromAbbreviation(abbreviation);

    /// <summary>Converts FileSizeUnit enum to FileSizeUnitInfo.</summary>
    /// <param name="fileSizeUnit">The FileSizeUnit enum value.</param>
    /// <returns>The corresponding FileSizeUnitInfo, or Unknown if not found.</returns>
    public static FileSizeUnitInfo ToFileSizeUnitInfo(this FileSizeUnit fileSizeUnit) => FileSizeUnitInfo.FromFileSizeUnit(fileSizeUnit);

    // GeographicInfo extension methods

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

    public static string ToHexString(this byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            sb.Append($"{b:x2}");

        return sb.ToString();
    }

    public static string ToMimeString(this MimeType mimeType)
    {
        var field = mimeType.GetType().GetField(mimeType.ToString());
        var attr = field?.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;
        return attr?.Description ?? FileTypeInfo.Unknown.MimeType;
    }

    /// <summary>Gets the audio format from a file extension.</summary>
    /// <param name="filePath">The file path or extension (with or without dot).</param>
    /// <returns>The audio format, or Unknown if not recognized.</returns>
    public static AudioFormat GetAudioFormatFromExtension(this string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return AudioFormat.Unknown;

        var extension = Path.GetExtension(filePath).TrimStart('.');
        if (string.IsNullOrWhiteSpace(extension))
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
        if (string.IsNullOrWhiteSpace(filePath))
            return MimeType.Unknown;

        var extension = Path.GetExtension(filePath).TrimStart('.');
        if (string.IsNullOrWhiteSpace(extension))
            extension = filePath.TrimStart('.');

        return Enum.TryParse<MimeType>(extension, out var result) ? result : MimeType.Unknown;
    }

    /// <summary>Gets the MIME type from a FileInfo.</summary>
    /// <param name="fileInfo">The file info.</param>
    /// <returns>The MIME type, or Unknown if not recognized.</returns>
    public static MimeType GetMimeTypeFromExtension(this FileInfo? fileInfo) => fileInfo == null ? MimeType.Unknown : fileInfo.FullName.GetMimeTypeFromExtension();

    public static bool IsSingleFlag<T>(this T value)
        where T : Enum
    {
        var intValue = Convert.ToInt64(value);
        return intValue != 0 && (intValue & (intValue - 1)) == 0;
    }
}