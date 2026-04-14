using System.Globalization;
using System.Reflection;
using Lyo.Common.Enums;

namespace Lyo.Common.Records;

/// <summary>Represents file size unit information with name, abbreviation, bytes multiplier, and description. Wraps FileSizeUnit enum.</summary>
public record FileSizeUnitInfo(string Name, string Abbreviation, string Description, long BytesMultiplier, FileSizeUnit FileSizeUnit)
{
    // Unknown
    public static readonly FileSizeUnitInfo Unknown = new("Unknown", "?", "Unknown file size unit", 0, (FileSizeUnit)(-1));

    // Units
    public static readonly FileSizeUnitInfo Byte = new("Byte", "B", "The basic unit of digital information", 1L, FileSizeUnit.B);
    public static readonly FileSizeUnitInfo Kilobyte = new("Kilobyte", "KB", "1,024 bytes", 1024L, FileSizeUnit.KB);
    public static readonly FileSizeUnitInfo Megabyte = new("Megabyte", "MB", "1,024 kilobytes (1,048,576 bytes)", 1024L * 1024, FileSizeUnit.MB);
    public static readonly FileSizeUnitInfo Gigabyte = new("Gigabyte", "GB", "1,024 megabytes (1,073,741,824 bytes)", 1024L * 1024 * 1024, FileSizeUnit.GB);
    public static readonly FileSizeUnitInfo Terabyte = new("Terabyte", "TB", "1,024 gigabytes (1,099,511,627,776 bytes)", 1024L * 1024 * 1024 * 1024, FileSizeUnit.TB);
    public static readonly FileSizeUnitInfo Petabyte = new("Petabyte", "PB", "1,024 terabytes (1,125,899,906,842,624 bytes)", 1024L * 1024 * 1024 * 1024 * 1024, FileSizeUnit.PB);

    public static readonly FileSizeUnitInfo Exabyte = new(
        "Exabyte", "EB", "1,024 petabytes (1,152,921,504,606,846,976 bytes)", 1024L * 1024 * 1024 * 1024 * 1024 * 1024, FileSizeUnit.EB);

    // Static registry with fast lookups
    private static readonly Dictionary<string, FileSizeUnitInfo> _byAbbreviation = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<FileSizeUnit, FileSizeUnitInfo> _byFileSizeUnit = new();
    private static readonly List<FileSizeUnitInfo> _allUnits = new();

    /// <summary>Gets all registered file size units.</summary>
    public static IReadOnlyList<FileSizeUnitInfo> All => _allUnits;

    static FileSizeUnitInfo()
    {
        // Register all units using reflection to find static fields
        var type = typeof(FileSizeUnitInfo);
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(FileSizeUnitInfo))
            .Select(f => (FileSizeUnitInfo)f.GetValue(null)!)
            .ToList();

        foreach (var unit in fields) {
            if (unit.FileSizeUnit == (FileSizeUnit)(-1)) // Skip Unknown
                continue;

            _allUnits.Add(unit);
            _byAbbreviation[unit.Abbreviation] = unit;
            _byFileSizeUnit[unit.FileSizeUnit] = unit;
        }
    }

    /// <summary>Finds a file size unit by its abbreviation.</summary>
    /// <param name="abbreviation">The unit abbreviation (e.g., "KB", "MB", "GB").</param>
    /// <returns>The file size unit info, or Unknown if not found.</returns>
    public static FileSizeUnitInfo FromAbbreviation(string? abbreviation)
    {
        if (string.IsNullOrWhiteSpace(abbreviation))
            return Unknown;

        return _byAbbreviation.TryGetValue(abbreviation!.Trim(), out var unit) ? unit : Unknown;
    }

    /// <summary>Finds a file size unit by FileSizeUnit enum.</summary>
    /// <param name="fileSizeUnit">The FileSizeUnit enum value.</param>
    /// <returns>The file size unit info, or Unknown if not found.</returns>
    public static FileSizeUnitInfo FromFileSizeUnit(FileSizeUnit fileSizeUnit) => _byFileSizeUnit.TryGetValue(fileSizeUnit, out var unit) ? unit : Unknown;

    /// <summary>Converts bytes to the unit's value.</summary>
    /// <param name="bytes">The number of bytes.</param>
    /// <returns>The value in this unit.</returns>
    public double ConvertFromBytes(long bytes) => bytes < 0 ? 0 : bytes / (double)BytesMultiplier;

    /// <summary>Gets the largest unit whose converted value from bytes is at least 1 (or Byte for 0/negative input).</summary>
    /// <param name="bytes">The number of bytes.</param>
    /// <returns>The best fit file size unit.</returns>
    public static FileSizeUnitInfo GetBestFitUnit(long bytes)
    {
        if (bytes <= 0)
            return Byte;

        return _allUnits.OrderBy(u => u.BytesMultiplier).LastOrDefault(u => bytes >= u.BytesMultiplier) ?? Byte;
    }

    /// <summary>Formats bytes using the best fit unit abbreviation (e.g., "1.82mb").</summary>
    /// <param name="bytes">The number of bytes.</param>
    /// <param name="decimals">The max decimal places for the formatted value.</param>
    /// <param name="lowercaseAbbreviation">Whether the unit abbreviation should be lowercased.</param>
    /// <returns>Formatted abbreviated size string.</returns>
    public static string FormatBestFitAbbreviation(long bytes, int decimals = 2, bool lowercaseAbbreviation = true)
    {
        var unit = GetBestFitUnit(bytes);
        var value = unit.ConvertFromBytes(bytes);
        var format = decimals <= 0 ? "0" : "0." + new string('#', decimals);
        var valueText = value.ToString(format, CultureInfo.InvariantCulture);
        var abbreviation = lowercaseAbbreviation ? unit.Abbreviation.ToLowerInvariant() : unit.Abbreviation;
        return valueText + abbreviation;
    }

    /// <summary>Converts a value in this unit to bytes.</summary>
    /// <param name="value">The value in this unit.</param>
    /// <returns>The number of bytes.</returns>
    public long ConvertToBytes(double value) => (long)(value * BytesMultiplier);

    /// <summary>Implicit conversion to FileSizeUnit for seamless integration.</summary>
    public static implicit operator FileSizeUnit(FileSizeUnitInfo info) => info.FileSizeUnit;

    /// <summary>Implicit conversion from FileSizeUnit for seamless integration.</summary>
    public static implicit operator FileSizeUnitInfo(FileSizeUnit fileSizeUnit) => FromFileSizeUnit(fileSizeUnit);
}