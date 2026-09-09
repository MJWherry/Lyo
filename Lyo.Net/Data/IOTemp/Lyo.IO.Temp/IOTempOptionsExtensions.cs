using Lyo.Common.Metadata.Records;
using Lyo.IO.Temp.Models;

namespace Lyo.IO.Temp;

/// <summary>Fluent helpers that set IOTemp options using <see cref="FileSizeUnitInfo" /> units.</summary>
public static class IOTempOptionsExtensions
{
    /// <summary>Sets <see cref="IOTempServiceOptions.MaxFileSizeBytes" /> and returns <paramref name="options" /> for chaining.</summary>
    public static IOTempServiceOptions WithMaxFileSize(this IOTempServiceOptions options, FileSizeUnitInfo unit, double amount)
    {
        options.MaxFileSizeBytes = unit.ConvertToBytes(amount);
        return options;
    }

    /// <summary>Sets <see cref="IOTempServiceOptions.MaxTotalSizeBytes" /> and returns <paramref name="options" /> for chaining.</summary>
    public static IOTempServiceOptions WithMaxTotalSize(this IOTempServiceOptions options, FileSizeUnitInfo unit, double amount)
    {
        options.MaxTotalSizeBytes = unit.ConvertToBytes(amount);
        return options;
    }

    /// <summary>Sets <see cref="IOTempServiceOptions.MaxFileCount" /> and returns <paramref name="options" /> for chaining.</summary>
    public static IOTempServiceOptions WithMaxFileCount(this IOTempServiceOptions options, int count)
    {
        options.MaxFileCount = count;
        return options;
    }

    /// <summary>Sets <see cref="IOTempServiceOptions.FileLifetime" /> and returns <paramref name="options" /> for chaining.</summary>
    public static IOTempServiceOptions WithFileLifetime(this IOTempServiceOptions options, TimeSpan lifetime)
    {
        options.FileLifetime = lifetime;
        return options;
    }

    /// <summary>Copy of <paramref name="options" /> with <c>MaxFileSizeBytes</c> set from the unit amount.</summary>
    public static IOTempSessionOptions WithMaxFileSize(this IOTempSessionOptions options, FileSizeUnitInfo unit, double amount) => CopyWith(options, unit.ConvertToBytes(amount));

    /// <summary>Copy of <paramref name="options" /> with <c>MaxTotalSizeBytes</c> set from the unit amount.</summary>
    public static IOTempSessionOptions WithMaxTotalSize(this IOTempSessionOptions options, FileSizeUnitInfo unit, double amount)
        => CopyWith(options, maxTotalSizeBytes: unit.ConvertToBytes(amount));

    /// <summary>Copy of <paramref name="options" /> with <c>MaxFileCount</c> set to <paramref name="count" />.</summary>
    public static IOTempSessionOptions WithMaxFileCount(this IOTempSessionOptions options, int count) => CopyWith(options, maxFileCount: count);

    /// <summary>Copy of <paramref name="options" /> with <c>FileLifetime</c> set to <paramref name="lifetime" />.</summary>
    public static IOTempSessionOptions WithFileLifetime(this IOTempSessionOptions options, TimeSpan lifetime) => CopyWith(options, fileLifetime: lifetime);

    private static IOTempSessionOptions
        CopyWith(IOTempSessionOptions src, long? maxFileSizeBytes = null, long? maxTotalSizeBytes = null, int? maxFileCount = null, TimeSpan? fileLifetime = null)
        => new() {
            RootDirectory = src.RootDirectory,
            EnableMetrics = src.EnableMetrics,
            FilePrefix = src.FilePrefix,
            FileSuffix = src.FileSuffix,
            FileExtension = src.FileExtension,
            FileNamingStrategy = src.FileNamingStrategy,
            DirectoryPrefix = src.DirectoryPrefix,
            DirectorySuffix = src.DirectorySuffix,
            DirectoryNamingStrategy = src.DirectoryNamingStrategy,
            MaxFileSizeBytes = maxFileSizeBytes ?? src.MaxFileSizeBytes,
            MaxTotalSizeBytes = maxTotalSizeBytes ?? src.MaxTotalSizeBytes,
            MaxFileCount = maxFileCount ?? src.MaxFileCount,
            FileLifetime = fileLifetime ?? src.FileLifetime,
            OverflowStrategy = src.OverflowStrategy
        };
}