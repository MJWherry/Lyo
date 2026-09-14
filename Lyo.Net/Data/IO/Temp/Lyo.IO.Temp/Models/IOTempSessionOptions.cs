using System.Globalization;
using Lyo.Common.Metadata.Records;
using Lyo.IO.Temp.Enums;
#if NETSTANDARD2_0
using System.Diagnostics;
#endif

namespace Lyo.IO.Temp.Models;

/// <summary>
/// Naming, layout, caps, and overflow for one session. Sessions from <see cref="Lyo.IO.Temp.IIOTempService.CreateSession" /> get <see cref="RootDirectory" /> set to the
/// service folder.
/// </summary>
// ReSharper disable once InconsistentNaming
public sealed class IOTempSessionOptions
{
    /// <summary>
    /// Default parent for sessions created without a service. Includes the process id so two runners or processes on one machine do not share a cleanup root, while still
    /// sitting under a discoverable <c>lyo-io-temp</c> folder.
    /// </summary>
    private static readonly string DefaultRootDirectory = Path.Combine(
        Path.GetTempPath(), "lyo-io-temp",
#if NET5_0_OR_GREATER
        Environment.ProcessId.ToString(CultureInfo.InvariantCulture));
#else
        Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture));
#endif

    /// <summary>Folder that will contain the session directory. Default is a per-process child of <see cref="Path.GetTempPath" />.</summary>
    public string RootDirectory { get; set; } = DefaultRootDirectory;

    /// <summary>
    /// If true (the default), a missing <see cref="RootDirectory" /> is created when the session starts; if false, a missing root throws. Same idea as
    /// <see cref="Lyo.IO.Temp.Models.IOTempServiceOptions" /> so standalone and service sessions bootstrap the same way.
    /// </summary>
    public bool CreateRootDirectoryIfNotExists { get; set; } = true;

    /// <summary>If true and the session has metrics, operations record timings and counters.</summary>
    public bool EnableMetrics { get; init; } = true;

    /// <summary>Optional prefix on generated file names in this session.</summary>
    public string? FilePrefix { get; init; }

    /// <summary>Optional suffix on generated file names in this session.</summary>
    public string? FileSuffix { get; init; }

    /// <summary>Extension added to generated file names.</summary>
    public string FileExtension { get; init; } = ".tmp";

    /// <summary>How the variable part of generated file names is chosen.</summary>
    public TempNamingStrategy FileNamingStrategy { get; init; } = TempNamingStrategy.Guid;

    /// <summary>Optional prefix on generated subdirectory names.</summary>
    public string? DirectoryPrefix { get; init; }

    /// <summary>Optional suffix on generated subdirectory names.</summary>
    public string? DirectorySuffix { get; init; }

    /// <summary>How the variable part of generated subdirectory names is chosen.</summary>
    public TempNamingStrategy DirectoryNamingStrategy { get; init; } = TempNamingStrategy.Guid;

    /// <summary>Cap in bytes for one new or appended file; null turns the check off.</summary>
    public long? MaxFileSizeBytes { get; init; } = FileSizeUnitInfo.Gigabyte.ConvertToBytes(1);

    /// <summary>
    /// Cap on total bytes of files in this session. When crossed, <see cref="OverflowStrategy" /> either deletes old/large files or throws.
    /// </summary>
    public long? MaxTotalSizeBytes { get; init; }

    /// <summary>
    /// Cap on how many tracked files this session may hold at once. When crossed, <see cref="OverflowStrategy" /> either deletes oldest/largest files or throws.
    /// </summary>
    public int? MaxFileCount { get; init; }

    /// <summary>
    /// Optional lifetime for files in this session. The cleanup worker and <see cref="IIOTempService.Cleanup()" /> use it when set; otherwise the service-level lifetime is used.
    /// </summary>
    public TimeSpan? FileLifetime { get; init; }

    /// <summary>What to do when <see cref="MaxFileCount" />, <see cref="MaxTotalSizeBytes" />, or a per-file cap would be crossed.</summary>
    public TempOverflowStrategy OverflowStrategy { get; init; } = TempOverflowStrategy.ThrowException;
}