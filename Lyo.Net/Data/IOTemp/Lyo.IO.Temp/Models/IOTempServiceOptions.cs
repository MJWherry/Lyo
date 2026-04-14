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

    public string RootDirectory { get; set; } = Path.Combine(Path.GetTempPath(), "lyo-io-temp");

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

    public TempOverflowStrategy OverflowStrategy { get; set; } = TempOverflowStrategy.ThrowException;

    public override string ToString() => $"{RootDirectory}/{DirectoryPrefix}*{DirectorySuffix}/{FilePrefix}*{FileSuffix}{FileExtension}";
}