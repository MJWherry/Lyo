using System.Globalization;
using Lyo.Common.Core;
using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Extensions;
using Lyo.Common.Metadata.Extensions;

namespace Lyo.Common.Metadata.Records;

/// <summary>Catalog row for a file-size unit: display name, abbreviation, byte multiplier, and the wrapped <see cref="FileSizeUnit" />.</summary>
public record FileSizeUnitInfo(string Name, string Abbreviation, string Description, long BytesMultiplier, FileSizeUnit FileSizeUnit)
{
    // Unrecognized unit
    public static readonly FileSizeUnitInfo Unknown = new("Unknown", "?", "Unknown file size unit", 0, (FileSizeUnit)(-1));

    // Named units
    public static readonly FileSizeUnitInfo Byte = new("Byte", "B", "The basic unit of digital information", 1L, FileSizeUnit.B);
    public static readonly FileSizeUnitInfo Kilobyte = new("Kilobyte", "KB", "1,024 bytes", 1024L, FileSizeUnit.KB);
    public static readonly FileSizeUnitInfo Megabyte = new("Megabyte", "MB", "1,024 kilobytes (1,048,576 bytes)", 1024L * 1024, FileSizeUnit.MB);
    public static readonly FileSizeUnitInfo Gigabyte = new("Gigabyte", "GB", "1,024 megabytes (1,073,741,824 bytes)", 1024L * 1024 * 1024, FileSizeUnit.GB);
    public static readonly FileSizeUnitInfo Terabyte = new("Terabyte", "TB", "1,024 gigabytes (1,099,511,627,776 bytes)", 1024L * 1024 * 1024 * 1024, FileSizeUnit.TB);
    public static readonly FileSizeUnitInfo Petabyte = new("Petabyte", "PB", "1,024 terabytes (1,125,899,906,842,624 bytes)", 1024L * 1024 * 1024 * 1024 * 1024, FileSizeUnit.PB);

    public static readonly FileSizeUnitInfo Exabyte = new(
        "Exabyte", "EB", "1,024 petabytes (1,152,921,504,606,846,976 bytes)", 1024L * 1024 * 1024 * 1024 * 1024 * 1024, FileSizeUnit.EB);

    // Lookup tables keyed by abbreviation and enum
    private static readonly Dictionary<string, FileSizeUnitInfo> ByAbbreviation = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<FileSizeUnit, FileSizeUnitInfo> ByFileSizeUnit = new();
    private static readonly List<FileSizeUnitInfo> AllUnits = [];

    /// <summary>Every registered file-size unit except the unknown sentinel.</summary>
    public static IReadOnlyList<FileSizeUnitInfo> All => AllUnits;

    static FileSizeUnitInfo()
    {
        var fields = typeof(FileSizeUnitInfo).PublicStaticFields<FileSizeUnitInfo>();

        foreach (var unit in fields) {
            if (unit.FileSizeUnit == (FileSizeUnit)(-1)) // Omit the unknown sentinel
                continue;

            AllUnits.Add(unit);
            ByAbbreviation[unit.Abbreviation] = unit;
            ByFileSizeUnit[unit.FileSizeUnit] = unit;
        }
    }

    /// <summary>Looks up a unit by abbreviation.</summary>
    /// <param name="abbreviation">Unit token, for example <c>KB</c>, <c>MB</c>, or <c>GB</c>.</param>
    /// <returns>The catalog row, or <see cref="Unknown" /> when the token is not registered.</returns>
    public static FileSizeUnitInfo FromAbbreviation(string? abbreviation)
        => abbreviation.IsNullOrEmpty() ? Unknown : ByAbbreviation.GetValueOrDefault(abbreviation.Trim(), Unknown)!;

    /// <summary>Looks up a unit from a <see cref="FileSizeUnit" /> member.</summary>
    /// <param name="fileSizeUnit">The <see cref="FileSizeUnit" /> value.</param>
    /// <returns>The catalog row, or <see cref="Unknown" /> when the member is not registered.</returns>
    public static FileSizeUnitInfo FromFileSizeUnit(FileSizeUnit fileSizeUnit) => ByFileSizeUnit.GetValueOrDefault(fileSizeUnit, Unknown)!;

    /// <summary>Expresses <paramref name="bytes" /> in this unit.</summary>
    /// <param name="bytes">Byte count.</param>
    /// <returns>The quantity in this unit.</returns>
    public double ConvertFromBytes(long bytes) => bytes < 0 ? 0 : bytes / (double)BytesMultiplier;

    /// <summary>Picks the largest unit whose converted value is at least 1 (or <see cref="Byte" /> for zero/negative input).</summary>
    /// <param name="bytes">Byte count.</param>
    /// <returns>The best-fit catalog row.</returns>
    public static FileSizeUnitInfo GetBestFitUnit(long bytes)
        => bytes <= 0 ? Byte : AllUnits.OrderBy(u => u.BytesMultiplier).LastOrDefault(u => bytes >= u.BytesMultiplier) ?? Byte;

    /// <summary>Renders <paramref name="bytes" /> with the best-fit abbreviation, for example <c>1.82mb</c>.</summary>
    /// <param name="bytes">Byte count.</param>
    /// <param name="decimals">Maximum fraction digits.</param>
    /// <param name="lowercaseAbbreviation">True when the unit token should be lowercased.</param>
    /// <returns>Compact size string.</returns>
    public static string FormatBestFitAbbreviation(long bytes, int decimals = 2, bool lowercaseAbbreviation = true)
    {
        var unit = GetBestFitUnit(bytes);
        var value = unit.ConvertFromBytes(bytes);
        var format = decimals <= 0 ? "0" : "0." + new string('#', decimals);
        var valueText = value.ToString(format, CultureInfo.InvariantCulture);
        var abbreviation = lowercaseAbbreviation ? unit.Abbreviation.ToLowerInvariant() : unit.Abbreviation;
        return valueText + abbreviation;
    }

    /// <summary>
    /// Renders <paramref name="bytes" /> for display with a space and the unit's canonical casing (for example <c>1.82 MB</c>). Prefer this for UI labels;
    /// <see cref="FormatBestFitAbbreviation" /> emits the compact <c>1.82mb</c> form for identifiers and logs.
    /// </summary>
    /// <param name="bytes">Byte count. Negative values are treated as zero.</param>
    /// <param name="decimals">Maximum fraction digits. Whole-byte values omit a fraction, because <c>512.00 B</c> reads worse than <c>512 B</c>.</param>
    public static string FormatBestFit(long bytes, int decimals = 2)
    {
        var unit = GetBestFitUnit(bytes);
        if (unit == Byte)
            return $"{Math.Max(bytes, 0)} {Byte.Abbreviation}";

        var format = decimals <= 0 ? "0" : "0." + new string('#', decimals);
        return $"{unit.ConvertFromBytes(bytes).ToString(format, CultureInfo.InvariantCulture)} {unit.Abbreviation}";
    }

    /// <summary>Converts a quantity in this unit back to bytes.</summary>
    /// <param name="value">Quantity in this unit.</param>
    /// <returns>Byte count.</returns>
    public long ConvertToBytes(double value) => (long)(value * BytesMultiplier);

    /// <summary>Implicit conversion to <see cref="FileSizeUnit" /> so options can take the catalog row.</summary>
    public static implicit operator FileSizeUnit(FileSizeUnitInfo info) => info.FileSizeUnit;

    /// <summary>Implicit conversion from <see cref="FileSizeUnit" /> so enum members resolve to catalog rows.</summary>
    public static implicit operator FileSizeUnitInfo(FileSizeUnit fileSizeUnit) => FromFileSizeUnit(fileSizeUnit);
}