using Lyo.Common.Records;

namespace Lyo.IO.Temp.Models;

/// <summary>
/// Fluent builder for <see cref="TempDirectorySpec"/>.
/// Obtain an instance via <see cref="TempDirectorySpec.Builder()"/>.
/// </summary>
public sealed class TempDirectorySpecBuilder
{
    private int _fileCount;
    private long _fileSizeBytes;
    private Func<int, long>? _fileSizeSelector;
    private readonly List<TempDirectorySpec> _subdirectories = [];

    internal TempDirectorySpecBuilder() { }

    /// <summary>Sets the number of files and uniform size in bytes.</summary>
    public TempDirectorySpecBuilder WithFiles(int count, long sizeBytes)
    {
        _fileCount = count;
        _fileSizeBytes = sizeBytes;
        return this;
    }

    /// <summary>Sets the number of files and uniform size using a <see cref="FileSizeUnitInfo"/> unit.</summary>
    public TempDirectorySpecBuilder WithFiles(int count, FileSizeUnitInfo unit, double amount)
        => WithFiles(count, unit.ConvertToBytes(amount));

    /// <summary>
    /// Sets a per-file size selector function. The function receives the 0-based file index
    /// and returns the size in bytes. Overrides <c>WithFiles</c> size when set.
    /// </summary>
    public TempDirectorySpecBuilder WithFileSizeSelector(Func<int, long> selector)
    {
        _fileSizeSelector = selector;
        return this;
    }

    /// <summary>Adds a subdirectory configured by a nested builder action.</summary>
    public TempDirectorySpecBuilder WithSubdirectory(Action<TempDirectorySpecBuilder> configure)
    {
        var sub = new TempDirectorySpecBuilder();
        configure(sub);
        _subdirectories.Add(sub.Build());
        return this;
    }

    /// <summary>Adds an already-built subdirectory spec.</summary>
    public TempDirectorySpecBuilder WithSubdirectory(TempDirectorySpec spec)
    {
        _subdirectories.Add(spec);
        return this;
    }

    public TempDirectorySpec Build()
        => new() {
            FileCount = _fileCount,
            FileSizeBytes = _fileSizeBytes,
            FileSizeSelector = _fileSizeSelector,
            Subdirectories = _subdirectories.Count > 0 ? _subdirectories.ToArray() : null
        };
}
