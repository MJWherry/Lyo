using System.Diagnostics;
using Lyo.Common.Records;
using Lyo.IO.Temp.Enums;

namespace Lyo.IO.Temp.Models;

/// <summary>Configuration for <see cref="Lyo.IO.Temp.IOTempService" />: temp root, naming defaults, limits, and cleanup age.</summary>
[DebuggerDisplay("{ToString(),nq}")]
// ReSharper disable once InconsistentNaming
public class IOTempServiceOptions
{
    /// <summary>Configuration section name for binding.</summary>
    public const string SectionName = "IOTempService";

    /// <summary>The OS temp root folder. Defaults to <see cref="Path.GetTempPath" />.</summary>
    public string TempRoot { get; set; } = Path.GetTempPath();

    /// <summary>
    /// The subdirectory name under <see cref="TempRoot" /> used by this service. Changing this per-instance (e.g. in tests) prevents collisions when multiple instances run in
    /// parallel.
    /// </summary>
    public string DirectoryName { get; set; } = "lyo-io-temp";

    /// <summary>Full root directory, computed from <see cref="TempRoot" /> and <see cref="DirectoryName" />.</summary>
    public string RootDirectory => Path.Combine(TempRoot, DirectoryName);

    /// <summary>When true, creates <see cref="RootDirectory" /> on startup if missing; otherwise a missing root throws.</summary>
    public bool CreateRootDirectoryIfNotExists { get; set; } = true;

    /// <summary>When true and an <see cref="Lyo.Metrics.IMetrics" /> instance is supplied to the service, operation timings and counters are recorded.</summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>Optional prefix for auto-generated one-off and session file names.</summary>
    public string? FilePrefix { get; set; }

    /// <summary>Strategy for the variable segment of auto-generated file names.</summary>
    public TempNamingStrategy FileNamingStrategy { get; set; } = TempNamingStrategy.Guid;

    /// <summary>Optional suffix for auto-generated file names.</summary>
    public string? FileSuffix { get; set; }

    /// <summary>Optional prefix for auto-generated directory names.</summary>
    public string? DirectoryPrefix { get; set; }

    /// <summary>Strategy for the variable segment of auto-generated directory names.</summary>
    public TempNamingStrategy DirectoryNamingStrategy { get; set; } = TempNamingStrategy.Guid;

    /// <summary>Optional suffix for auto-generated directory names.</summary>
    public string? DirectorySuffix { get; set; }

    /// <summary>Extension appended to auto-generated file names (e.g. <c>.tmp</c>).</summary>
    public string FileExtension { get; set; } = ".tmp";

    /// <summary>Default minimum age for parameterless <see cref="Lyo.IO.Temp.IIOTempService.Cleanup()" />; null uses a zero threshold.</summary>
    public TimeSpan? FileLifetime { get; set; }

    /// <summary>Default per-file size limit for new sessions and one-off files.</summary>
    public long MaxFileSizeBytes { get; set; } = FileSizeUnitInfo.Gigabyte.ConvertToBytes(1);

    /// <summary>Not a hard cap on disk usage by itself; inherited as session default and used where applicable.</summary>
    public long MaxTotalSizeBytes { get; set; } = FileSizeUnitInfo.Gigabyte.ConvertToBytes(10);

    /// <summary>
    /// Maximum number of tracked files allowed per session. When exceeded, <see cref="OverflowStrategy" /> governs whether the oldest/largest files are deleted or an exception
    /// is thrown. <c>null</c> means no limit.
    /// </summary>
    public int? MaxFileCount { get; set; }

    /// <summary>Default overflow behaviour for new sessions when limits are exceeded.</summary>
    public TempOverflowStrategy OverflowStrategy { get; set; } = TempOverflowStrategy.ThrowException;

    /// <summary>Debug-oriented summary of configured roots and naming patterns.</summary>
    public override string ToString() => $"{TempRoot}/{DirectoryName}/{DirectoryPrefix}*{DirectorySuffix}/{FilePrefix}*{FileSuffix}{FileExtension}";
}