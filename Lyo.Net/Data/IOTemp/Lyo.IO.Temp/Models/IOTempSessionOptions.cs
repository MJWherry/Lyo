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

    /// <summary>
    /// Maximum total bytes for all files in this session.
    /// When exceeded, <see cref="OverflowStrategy"/> governs whether old/large files are deleted or an exception is thrown.
    /// </summary>
    public long? MaxTotalSizeBytes { get; init; }

    /// <summary>
    /// Maximum number of tracked files allowed in this session at any one time.
    /// When exceeded, <see cref="OverflowStrategy"/> governs whether the oldest/largest files are deleted or an exception is thrown.
    /// </summary>
    public int? MaxFileCount { get; init; }

    /// <summary>
    /// Optional target lifetime for files in this session.
    /// The cleanup worker and <see cref="IIOTempService.Cleanup()"/> will honour this when set; otherwise the service-level lifetime applies.
    /// </summary>
    public TimeSpan? FileLifetime { get; init; }

    public TempOverflowStrategy OverflowStrategy { get; init; } = TempOverflowStrategy.ThrowException;
}