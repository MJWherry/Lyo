using Lyo.Common.Records;
using Lyo.IO.Temp.Enums;

namespace Lyo.IO.Temp.Models;

/// <summary>
/// Per-session layout, naming, limits, and overflow behaviour. When created via <see cref="Lyo.IO.Temp.IIOTempService.CreateSession" />, <see cref="RootDirectory" /> is set
/// to the service directory.
/// </summary>
// ReSharper disable once InconsistentNaming
public sealed class IOTempSessionOptions
{
    /// <summary>Parent directory under which the session folder is created.</summary>
    public string RootDirectory { get; set; } = Path.Combine(Path.GetTempPath(), "lyo-io-temp");

    /// <summary>When true and metrics are supplied to the session, operations emit timings and counters.</summary>
    public bool EnableMetrics { get; init; } = true;

    /// <summary>Optional prefix for auto-generated file names in this session.</summary>
    public string? FilePrefix { get; init; }

    /// <summary>Optional suffix for auto-generated file names in this session.</summary>
    public string? FileSuffix { get; init; }

    /// <summary>Extension appended to auto-generated file names.</summary>
    public string FileExtension { get; init; } = ".tmp";

    /// <summary>Strategy for the variable segment of auto-generated file names.</summary>
    public TempNamingStrategy FileNamingStrategy { get; init; } = TempNamingStrategy.Guid;

    /// <summary>Optional prefix for auto-generated subdirectory names.</summary>
    public string? DirectoryPrefix { get; init; }

    /// <summary>Optional suffix for auto-generated subdirectory names.</summary>
    public string? DirectorySuffix { get; init; }

    /// <summary>Strategy for the variable segment of auto-generated subdirectory names.</summary>
    public TempNamingStrategy DirectoryNamingStrategy { get; init; } = TempNamingStrategy.Guid;

    /// <summary>Maximum size in bytes for a single new or appended file; null disables the check.</summary>
    public long? MaxFileSizeBytes { get; init; } = FileSizeUnitInfo.Gigabyte.ConvertToBytes(1);

    /// <summary>Maximum total bytes for all files in this session. When exceeded, <see cref="OverflowStrategy" /> governs whether old/large files are deleted or an exception is thrown.</summary>
    public long? MaxTotalSizeBytes { get; init; }

    /// <summary>
    /// Maximum number of tracked files allowed in this session at any one time. When exceeded, <see cref="OverflowStrategy" /> governs whether the oldest/largest files are
    /// deleted or an exception is thrown.
    /// </summary>
    public int? MaxFileCount { get; init; }

    /// <summary>
    /// Optional target lifetime for files in this session. The cleanup worker and <see cref="IIOTempService.Cleanup()" /> will honour this when set; otherwise the service-level
    /// lifetime applies.
    /// </summary>
    public TimeSpan? FileLifetime { get; init; }

    /// <summary>How to react when <see cref="MaxFileCount" />, <see cref="MaxTotalSizeBytes" />, or per-file limits would be exceeded.</summary>
    public TempOverflowStrategy OverflowStrategy { get; init; } = TempOverflowStrategy.ThrowException;
}