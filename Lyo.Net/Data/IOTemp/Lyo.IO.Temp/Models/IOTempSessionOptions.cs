using Lyo.Common.Records;
using Lyo.IO.Temp.Enums;

namespace Lyo.IO.Temp.Models;

// ReSharper disable once InconsistentNaming
public sealed class IOTempSessionOptions
{
    public string RootDirectory { get; set; } = Path.Combine(Path.GetTempPath(), "lyo-io-temp");

    public bool EnableMetrics { get; init; } = true;

    // Naming
    public string? FilePrefix { get; init; }

    public string? FileSuffix { get; init; }

    public string FileExtension { get; init; } = ".tmp";

    public TempNamingStrategy FileNamingStrategy { get; init; } = TempNamingStrategy.Guid;

    public string? DirectoryPrefix { get; init; }

    public string? DirectorySuffix { get; init; }

    public TempNamingStrategy DirectoryNamingStrategy { get; init; } = TempNamingStrategy.Guid;

    public long? MaxFileSizeBytes { get; init; } = FileSizeUnitInfo.Gigabyte.ConvertToBytes(1);

    public TempOverflowStrategy OverflowStrategy { get; init; } = TempOverflowStrategy.ThrowException;
}