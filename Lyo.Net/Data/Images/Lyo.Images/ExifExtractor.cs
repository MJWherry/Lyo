using System.Globalization;
using Lyo.Images.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using Rational = SixLabors.ImageSharp.Rational;

namespace Lyo.Images;

/// <summary>Extracts structured EXIF metadata from ImageSharp images.</summary>
internal static class ExifExtractor
{
    private const string ExifDateTimeFormat = "yyyy:MM:dd HH:mm:ss";

    /// <summary>Extracts EXIF metadata from an image.</summary>
    public static (ImageExifInfo? ExifInfo, Dictionary<string, string>? ExifData) Extract(Image image)
    {
        try {
            var exif = image.Metadata?.ExifProfile;
            if (exif == null)
                return (null, null);

            var make = GetString(exif, ExifTag.Make);
            var model = GetString(exif, ExifTag.Model);
            var software = GetString(exif, ExifTag.Software);
            var dateTimeOriginal = GetDateTime(exif, ExifTag.DateTimeOriginal);
            var dateTimeDigitized = GetDateTime(exif, ExifTag.DateTimeDigitized);
            var (lat, lon) = GetGpsCoordinates(exif);
            var altitude = GetGpsAltitude(exif);
            var exposureTime = GetRational(exif, ExifTag.ExposureTime);
            var fNumber = GetRational(exif, ExifTag.FNumber);
            var iso = GetIsoSpeed(exif);
            var orientation = GetShort(exif, ExifTag.Orientation);
            var hasAny = make != null || model != null || software != null || dateTimeOriginal != null || dateTimeDigitized != null || lat != null || lon != null ||
                altitude != null || exposureTime != null || fNumber != null || iso != null || orientation != null;

            var exifInfo = hasAny
                ? new ImageExifInfo(
                    make, model, software, dateTimeOriginal, dateTimeDigitized, lat, lon, altitude, RationalToDouble(exposureTime), RationalToDouble(fNumber), iso, orientation)
                : null;

            var exifData = BuildRawExifData(exif);
            return (exifInfo, exifData);
        }
        catch {
            return (null, null);
        }
    }

    private static string? GetString(ExifProfile exif, ExifTag<string> tag)
    {
        if (!exif.TryGetValue(tag, out var value) || value.Value == null)
            return null;

        var s = value.Value.Trim();
        return string.IsNullOrEmpty(s) ? null : s;
    }

    private static DateTime? GetDateTime(ExifProfile exif, ExifTag<string> tag)
    {
        var s = GetString(exif, tag);
        if (string.IsNullOrEmpty(s))
            return null;

        if (DateTime.TryParseExact(s, ExifDateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;

        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            return dt;

        return null;
    }

    private static (double? lat, double? lon) GetGpsCoordinates(ExifProfile exif)
    {
        if (!exif.TryGetValue(ExifTag.GPSLatitude, out var latValue) || latValue.Value == null || !exif.TryGetValue(ExifTag.GPSLongitude, out var lonValue) ||
            lonValue.Value == null)
            return (null, null);

        var lat = RationalArrayToDecimalDegrees(latValue.Value);
        var lon = RationalArrayToDecimalDegrees(lonValue.Value);
        if (lat == null || lon == null)
            return (null, null);

        var latRef = GetString(exif, ExifTag.GPSLatitudeRef);
        var lonRef = GetString(exif, ExifTag.GPSLongitudeRef);
        if (latRef?.Equals("S", StringComparison.OrdinalIgnoreCase) == true)
            lat = -lat.Value;

        if (lonRef?.Equals("W", StringComparison.OrdinalIgnoreCase) == true)
            lon = -lon.Value;

        return (lat, lon);
    }

    private static double? RationalArrayToDecimalDegrees(Rational[]? parts)
    {
        if (parts == null || parts.Length < 3)
            return null;

        var degrees = RationalToDouble(parts[0]) ?? 0;
        var minutes = RationalToDouble(parts[1]) ?? 0;
        var seconds = RationalToDouble(parts[2]) ?? 0;
        return degrees + minutes / 60.0 + seconds / 3600.0;
    }

    private static double? RationalToDouble(Rational? r)
    {
        if (!r.HasValue)
            return null;

        return RationalToDouble(r.Value);
    }

    private static double? RationalToDouble(Rational r) => r.Denominator != 0 ? (double)r.Numerator / r.Denominator : null;

    private static double? GetGpsAltitude(ExifProfile exif)
    {
        if (!exif.TryGetValue(ExifTag.GPSAltitude, out var value))
            return null;

        var alt = RationalToDouble(value.Value);
        if (alt == null)
            return null;

        if (exif.TryGetValue(ExifTag.GPSAltitudeRef, out var refValue)) {
            var refVal = refValue.Value; // 0 = above sea level, 1 = below
            if (refVal == 1)
                alt = -alt.Value;
        }

        return alt;
    }

    private static Rational? GetRational(ExifProfile exif, ExifTag<Rational> tag)
    {
        if (!exif.TryGetValue(tag, out var value))
            return null;

        var v = value.Value;
        if (v.Denominator == 0)
            return null;

        return v;
    }

    private static ushort? GetIsoSpeed(ExifProfile exif)
    {
        if (!exif.TryGetValue(ExifTag.ISOSpeedRatings, out var value) || value.Value == null || value.Value.Length == 0)
            return null;

        return value.Value[0];
    }

    private static ushort? GetShort(ExifProfile exif, ExifTag<ushort> tag)
    {
        if (!exif.TryGetValue(tag, out var value))
            return null;

        return value.Value;
    }

    private static Dictionary<string, string>? BuildRawExifData(ExifProfile exif)
    {
        if (exif.Values == null || exif.Values.Count == 0)
            return null;

        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var val in exif.Values) {
            try {
                var name = val.Tag.ToString();
                var str = val.GetValue()?.ToString();
                if (!string.IsNullOrEmpty(name) && str != null)
                    dict[name] = str;
            }
            catch {
                // Skip values that cannot be converted to string
            }
        }

        return dict.Count > 0 ? dict : null;
    }
}