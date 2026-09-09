using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;

namespace Lyo.IO.Temp.Models;

/// <summary>Shape of a fake temp directory: file count, size, and nested children.</summary>
public record TempDirectorySpec
{
    [ThreadStatic]
    private static Random? _rng;

    /// <summary>How many random files to place directly in this directory.</summary>
    public int FileCount { get; init; }

    /// <summary>Same byte length for every generated file. Unused when <see cref="FileSizeSelector" /> is set.</summary>
    public long FileSizeBytes { get; init; }

    /// <summary>
    /// Optional size function. Gets the 0-based file index and returns bytes. When set, it replaces <see cref="FileSizeBytes" /> for every file at this level.
    /// </summary>
    public Func<int, long>? FileSizeSelector { get; init; }

    /// <summary>Optional nested folders, each a <see cref="TempDirectorySpec" /> of its own.</summary>
    public IReadOnlyList<TempDirectorySpec>? Subdirectories { get; init; }

    /// <summary>Starts a <see cref="TempDirectorySpecBuilder" /> for fluent setup.</summary>
    public static TempDirectorySpecBuilder Builder() => new();

    /// <summary>Flat spec (no children) with <paramref name="fileCount" /> files of <paramref name="fileSizeBytes" /> bytes each.</summary>
    public static TempDirectorySpec Flat(int fileCount, long fileSizeBytes) => new() { FileCount = fileCount, FileSizeBytes = fileSizeBytes };

    /// <summary>Flat spec using a <see cref="FileSizeUnitInfo" /> unit and amount.</summary>
    public static TempDirectorySpec Flat(int fileCount, FileSizeUnitInfo unit, double amount) => Flat(fileCount, unit.ConvertToBytes(amount));

    /// <summary>
    /// Flat spec with file count and each file's size drawn uniformly from the given ranges. <see cref="FileSizeSelector" /> is set so each call picks an independent size in
    /// [<paramref name="minSize" />, <paramref name="maxSize" />].
    /// </summary>
    public static TempDirectorySpec Random(int minFiles, int maxFiles, long minSize, long maxSize)
    {
        ArgumentHelpers.ThrowIfNegative(minFiles);
        ArgumentHelpers.ThrowIfLessThan(maxFiles, minFiles);
        ArgumentHelpers.ThrowIfNegative(minSize);
        ArgumentHelpers.ThrowIfLessThan(maxSize, minSize);
        var rng = GetRandom();
        var fileCount = rng.Next(minFiles, maxFiles + 1);
        var sizeRange = maxSize - minSize;
        return new() { FileCount = fileCount, FileSizeBytes = minSize, FileSizeSelector = _ => minSize + (sizeRange == 0 ? 0 : (long)(GetRandom().NextDouble() * sizeRange)) };
    }

    /// <summary>Flat spec with random file count and sizes, using <see cref="FileSizeUnitInfo" /> units.</summary>
    public static TempDirectorySpec Random(int minFiles, int maxFiles, FileSizeUnitInfo minUnit, double minAmount, FileSizeUnitInfo maxUnit, double maxAmount)
        => Random(minFiles, maxFiles, minUnit.ConvertToBytes(minAmount), maxUnit.ConvertToBytes(maxAmount));

    private static Random GetRandom() => _rng ??= new();
}