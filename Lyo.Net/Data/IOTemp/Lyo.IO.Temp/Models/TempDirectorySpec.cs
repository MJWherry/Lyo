using Lyo.Common.Records;
using Lyo.Exceptions;

namespace Lyo.IO.Temp.Models;

/// <summary>Describes the structure of a simulated temp directory: how many files to create, their size, and any nested subdirectories.</summary>
public record TempDirectorySpec
{
    /// <summary>Number of random files to create directly in this directory.</summary>
    public int FileCount { get; init; }

    /// <summary>Uniform size in bytes for each generated file. Ignored when <see cref="FileSizeSelector" /> is set.</summary>
    public long FileSizeBytes { get; init; }

    /// <summary>
    /// Optional per-file size selector. Receives the 0-based file index and returns the desired size in bytes. When set, overrides <see cref="FileSizeBytes" /> for every file in
    /// this directory level.
    /// </summary>
    public Func<int, long>? FileSizeSelector { get; init; }

    /// <summary>Optional nested subdirectories, each described by their own <see cref="TempDirectorySpec" />.</summary>
    public IReadOnlyList<TempDirectorySpec>? Subdirectories { get; init; }

    /// <summary>Returns a new <see cref="TempDirectorySpecBuilder" /> for fluent construction.</summary>
    public static TempDirectorySpecBuilder Builder() => new();

    /// <summary>Creates a flat (no subdirectories) spec with <paramref name="fileCount" /> files of <paramref name="fileSizeBytes" /> bytes each.</summary>
    public static TempDirectorySpec Flat(int fileCount, long fileSizeBytes) => new() { FileCount = fileCount, FileSizeBytes = fileSizeBytes };

    /// <summary>Creates a flat spec using a <see cref="FileSizeUnitInfo" /> unit and an amount.</summary>
    public static TempDirectorySpec Flat(int fileCount, FileSizeUnitInfo unit, double amount) => Flat(fileCount, unit.ConvertToBytes(amount));

    /// <summary>
    /// Creates a flat spec where the file count and each file's size are chosen uniformly at random from the given ranges. The <see cref="FileSizeSelector" /> is set so every
    /// call returns an independent random size within [<paramref name="minSize" />, <paramref name="maxSize" />].
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

    /// <summary>Creates a flat spec with randomised file count and sizes using <see cref="FileSizeUnitInfo" /> units.</summary>
    public static TempDirectorySpec Random(int minFiles, int maxFiles, FileSizeUnitInfo minUnit, double minAmount, FileSizeUnitInfo maxUnit, double maxAmount)
        => Random(minFiles, maxFiles, minUnit.ConvertToBytes(minAmount), maxUnit.ConvertToBytes(maxAmount));
    
    [ThreadStatic]
    private static Random? _rng;

    private static Random GetRandom() => _rng ??= new();

}