using Lyo.Common.Metadata.Records;

namespace Lyo.IO.Temp.Models;

/// <summary>Fluent builder for <see cref="TempDirectorySpec" />. Get one from <see cref="TempDirectorySpec.Builder()" />.</summary>
public sealed class TempDirectorySpecBuilder
{
    private readonly List<TempDirectorySpec> _subdirectories = [];
    private int _fileCount;
    private long _fileSizeBytes;
    private Func<int, long>? _fileSizeSelector;

    internal TempDirectorySpecBuilder() { }

    /// <summary>Sets file count and a uniform size in bytes.</summary>
    public TempDirectorySpecBuilder WithFiles(int count, long sizeBytes)
    {
        _fileCount = count;
        _fileSizeBytes = sizeBytes;
        return this;
    }

    /// <summary>Sets file count and a uniform size from a <see cref="FileSizeUnitInfo" /> unit.</summary>
    public TempDirectorySpecBuilder WithFiles(int count, FileSizeUnitInfo unit, double amount) => WithFiles(count, unit.ConvertToBytes(amount));

    /// <summary>
    /// Sets a per-file size function. It receives the 0-based index and returns bytes. When set, it overrides the size from <c>WithFiles</c>.
    /// </summary>
    public TempDirectorySpecBuilder WithFileSizeSelector(Func<int, long> selector)
    {
        _fileSizeSelector = selector;
        return this;
    }

    /// <summary>Adds a child directory configured by a nested builder action.</summary>
    public TempDirectorySpecBuilder WithSubdirectory(Action<TempDirectorySpecBuilder> configure)
    {
        var sub = new TempDirectorySpecBuilder();
        configure(sub);
        _subdirectories.Add(sub.Build());
        return this;
    }

    /// <summary>Adds a subdirectory that is already built.</summary>
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