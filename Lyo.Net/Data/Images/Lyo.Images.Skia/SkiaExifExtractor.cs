using System.Globalization;
using Lyo.Images.Models;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Directory = MetadataExtractor.Directory;

namespace Lyo.Images.Skia;

/// <summary>Extracts EXIF metadata from image streams using MetadataExtractor (used when SkiaSharp does not provide EXIF).</summary>
internal static class SkiaExifExtractor
{
    /// <summary>Extracts EXIF metadata from a seekable image stream. Call after resetting stream position to 0.</summary>
    public static (ImageExifInfo? ExifInfo, Dictionary<string, string>? ExifData) Extract(Stream stream)
    {
        try {
            var directories = ImageMetadataReader.ReadMetadata(stream);
            if (directories == null || directories.Count == 0)
                return (null, null);

            var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
            var subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            var gps = directories.OfType<GpsDirectory>().FirstOrDefault();
            var make = GetString(ifd0, ExifDirectoryBase.TagMake);
            var model = GetString(ifd0, ExifDirectoryBase.TagModel);
            var software = GetString(ifd0, ExifDirectoryBase.TagSoftware);
            var dateTimeOriginal = GetDateTime(subIfd, ExifDirectoryBase.TagDateTimeOriginal);
            var dateTimeDigitized = GetDateTime(subIfd, ExifDirectoryBase.TagDateTimeDigitized);
            var (lat, lon) = GetGpsCoordinates(gps);
            var altitude = GetGpsAltitude(gps);
            var exposureTime = ParseRational(GetString(subIfd, ExifDirectoryBase.TagExposureTime));
            var fNumber = ParseRational(GetString(subIfd, ExifDirectoryBase.TagFNumber));
            var iso = GetShort(subIfd, ExifDirectoryBase.TagIsoEquivalent);
            var orientation = GetShort(ifd0, ExifDirectoryBase.TagOrientation);
            var hasAny = make != null || model != null || software != null || dateTimeOriginal != null || dateTimeDigitized != null || lat != null || lon != null ||
                altitude != null || exposureTime != null || fNumber != null || iso != null || orientation != null;

            var exifInfo = hasAny
                ? new ImageExifInfo(make, model, software, dateTimeOriginal, dateTimeDigitized, lat, lon, altitude, exposureTime, fNumber, iso, orientation)
                : null;

            var exifData = BuildRawExifData(directories);
            return (exifInfo, exifData);
        }
        catch {
            return (null, null);
        }
    }

    private static string? GetString(Directory? dir, int tag)
    {
        if (dir == null || !dir.ContainsTag(tag))
            return null;

        try {
            var value = dir.GetString(tag);
            var s = value?.Trim();
            return string.IsNullOrEmpty(s) ? null : s;
        }
        catch {
            return null;
        }
    }

    private static DateTime? GetDateTime(Directory? dir, int tag)
    {
        var s = GetString(dir, tag);
        if (string.IsNullOrEmpty(s))
            return null;

        if (DateTime.TryParseExact(s, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;

        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            return dt;

        return null;
    }

    private static (double? lat, double? lon) GetGpsCoordinates(GpsDirectory? gps)
    {
        if (gps == null || !gps.TryGetGeoLocation(out var geo))
            return (null, null);

        return (geo.Latitude, geo.Longitude);
    }

    private static double? GetGpsAltitude(GpsDirectory? gps)
    {
        if (gps == null || !gps.TryGetDouble(GpsDirectory.TagAltitude, out var alt))
            return null;

        if (gps.TryGetInt32(GpsDirectory.TagAltitudeRef, out var altRef) && altRef == 1)
            alt = -alt;

        return alt;
    }

    private static double? ParseRational(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return null;

        if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            return d;

        if (s.Contains('/')) {
            var parts = s.Split('/');
            if (parts.Length == 2 && double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var num) &&
                double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var den) && den != 0)
                return num / den;
        }

        return null;
    }

    private static ushort? GetShort(Directory? dir, int tag)
    {
        if (dir == null || !dir.TryGetInt32(tag, out var value))
            return null;

        return value is >= 0 and <= ushort.MaxValue ? (ushort)value : null;
    }

    private static Dictionary<string, string>? BuildRawExifData(IReadOnlyList<Directory> directories)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var dir in directories) {
            foreach (var tag in dir.Tags) {
                try {
                    var name = $"{dir.Name} - {tag.Name}";
                    var desc = tag.Description;
                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(desc))
                        dict[name] = desc;
                }
                catch {
                    // Skip values that cannot be converted
                }
            }
        }

        return dict.Count > 0 ? dict : null;
    }
}