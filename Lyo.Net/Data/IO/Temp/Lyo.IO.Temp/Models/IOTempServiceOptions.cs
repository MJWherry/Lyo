using System.Diagnostics;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;
using Lyo.IO.Temp.Enums;

namespace Lyo.IO.Temp.Models;

/// <summary>Settings for <see cref="Lyo.IO.Temp.IOTempService" />: temp root, default names, caps, and cleanup age.</summary>
[DebuggerDisplay("{ToString(),nq}")]
// ReSharper disable once InconsistentNaming
public class IOTempServiceOptions
{
    /// <summary>Section name used when binding configuration.</summary>
    public const string SectionName = "IOTempService";

    /// <summary>OS temp folder. Default is <see cref="Path.GetTempPath" />.</summary>
    public string TempRoot { get; set; } = Path.GetTempPath();

    /// <summary>
    /// Folder name under <see cref="TempRoot" /> for this service. Change it per instance (tests especially) so parallel instances do not collide.
    /// </summary>
    public string DirectoryName { get; set; } = "lyo-io-temp";

    /// <summary>Combined root from <see cref="TempRoot" /> and <see cref="DirectoryName" />. Used when <see cref="StorageKind" /> is <see cref="IOTempStorageKind.Local" />.</summary>
    public string RootDirectory => Path.Combine(TempRoot, DirectoryName);

    /// <summary>
    /// Backend used when DI does not already have an <see cref="Lyo.IO.FileSystem.IFileSystem" />. Default is local disk at <see cref="RootDirectory" />.
    /// </summary>
    public IOTempStorageKind StorageKind { get; set; } = IOTempStorageKind.Local;

    /// <summary>If true, a missing <see cref="RootDirectory" /> is created at startup; if false, a missing root throws.</summary>
    public bool CreateRootDirectoryIfNotExists { get; set; } = true;

    /// <summary>If true and the service has <see cref="Lyo.Metrics.IMetrics" />, operations record timings and counters.</summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>Optional prefix on generated one-off and session file names.</summary>
    public string? FilePrefix { get; set; }

    /// <summary>How the variable part of generated file names is chosen.</summary>
    public TempNamingStrategy FileNamingStrategy { get; set; } = TempNamingStrategy.Guid;

    /// <summary>Optional suffix on generated file names.</summary>
    public string? FileSuffix { get; set; }

    /// <summary>Optional prefix on generated directory names.</summary>
    public string? DirectoryPrefix { get; set; }

    /// <summary>How the variable part of generated directory names is chosen.</summary>
    public TempNamingStrategy DirectoryNamingStrategy { get; set; } = TempNamingStrategy.Guid;

    /// <summary>Optional suffix on generated directory names.</summary>
    public string? DirectorySuffix { get; set; }

    /// <summary>Extension added to generated file names (for example <c>.tmp</c>).</summary>
    public string FileExtension { get; set; } = ".tmp";

    /// <summary>Default minimum age for parameterless <see cref="Lyo.IO.Temp.IIOTempService.Cleanup()" />. Null means a zero cutoff.</summary>
    public TimeSpan? FileLifetime { get; set; }

    /// <summary>Default per-file size cap for new sessions and one-off files.</summary>
    public long MaxFileSizeBytes { get; set; } = FileSizeUnitInfo.Gigabyte.ConvertToBytes(1);

    /// <summary>Not a hard disk quota by itself; copied onto sessions as a default and used where those sessions apply it.</summary>
    public long MaxTotalSizeBytes { get; set; } = FileSizeUnitInfo.Gigabyte.ConvertToBytes(10);

    /// <summary>
    /// Cap on tracked files per session. When crossed, <see cref="OverflowStrategy" /> either deletes oldest/largest files or throws. <c>null</c> means no cap.
    /// </summary>
    public int? MaxFileCount { get; set; }

    /// <summary>Default overflow policy for new sessions when a cap is crossed.</summary>
    public TempOverflowStrategy OverflowStrategy { get; set; } = TempOverflowStrategy.ThrowException;

    /// <summary>Throws when roots, sizes, or naming enums are invalid.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(TempRoot);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(DirectoryName);
        ArgumentHelpers.ThrowIfNotDefined(StorageKind);
        ArgumentHelpers.ThrowIfNotDefined(FileNamingStrategy);
        ArgumentHelpers.ThrowIfNotDefined(DirectoryNamingStrategy);
        ArgumentHelpers.ThrowIfNotDefined(OverflowStrategy);
        ArgumentHelpers.ThrowIfNegativeOrZero(MaxFileSizeBytes);
        ArgumentHelpers.ThrowIfNegativeOrZero(MaxTotalSizeBytes);
        if (MaxFileCount is { } count)
            ArgumentHelpers.ThrowIfNegativeOrZero(count);
        if (FileLifetime is { } lifetime)
            ArgumentHelpers.ThrowIf(lifetime <= TimeSpan.Zero, "FileLifetime must be greater than zero.");
    }

    /// <summary>Short debug view of roots and naming patterns.</summary>
    public override string ToString() => $"{TempRoot}/{DirectoryName}/{DirectoryPrefix}*{DirectorySuffix}/{FilePrefix}*{FileSuffix}{FileExtension}";
}