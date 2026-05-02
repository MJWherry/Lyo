using System.Diagnostics;
using System.Text.Json.Serialization;
using Lyo.Common.Records;
using Lyo.IO.Temp.Enums;

namespace Lyo.IO.Temp.Models;

[DebuggerDisplay("{ToString(),nq}")]
// ReSharper disable once InconsistentNaming
public class IOTempServiceOptions
{
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

    public bool CreateRootDirectoryIfNotExists { get; set; } = true;

    public bool EnableMetrics { get; set; } = true;

    public string? FilePrefix { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TempNamingStrategy FileNamingStrategy { get; set; } = TempNamingStrategy.Guid;

    public string? FileSuffix { get; set; }

    public string? DirectoryPrefix { get; set; }

    public TempNamingStrategy DirectoryNamingStrategy { get; set; } = TempNamingStrategy.Guid;

    public string? DirectorySuffix { get; set; }

    public string FileExtension { get; set; } = ".tmp";

    public TimeSpan? FileLifetime { get; set; }

    public long MaxFileSizeBytes { get; set; } = FileSizeUnitInfo.Gigabyte.ConvertToBytes(1);

    public long MaxTotalSizeBytes { get; set; } = FileSizeUnitInfo.Gigabyte.ConvertToBytes(10);

    /// <summary>
    /// Maximum number of tracked files allowed per session. When exceeded, <see cref="OverflowStrategy" /> governs whether the oldest/largest files are deleted or an exception
    /// is thrown. <c>null</c> means no limit.
    /// </summary>
    public int? MaxFileCount { get; set; }

    public TempOverflowStrategy OverflowStrategy { get; set; } = TempOverflowStrategy.ThrowException;

    public override string ToString() => $"{TempRoot}/{DirectoryName}/{DirectoryPrefix}*{DirectorySuffix}/{FilePrefix}*{FileSuffix}{FileExtension}";
}