using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;

namespace Lyo.FileStorage.Models;

/// <summary>Limits for <see cref="IFileStorageArchiveService" /> zip downloads. Bind from configuration section <see cref="SectionName" />.</summary>
public sealed class FileStorageArchiveOptions
{
    /// <summary>Configuration section name. Starts as <c>FileStorageArchive</c>.</summary>
    public const string SectionName = "FileStorageArchive";

    /// <summary>Max files in one archive. Starts at 100.</summary>
    public int MaxFileCount { get; set; } = 100;

    /// <summary>Max sum of <c>OriginalFileSize</c> across entries, checked before any download. Starts at 256 MiB.</summary>
    public long MaxTotalUncompressedBytes { get; set; } = FileSizeUnitInfo.Megabyte.ConvertToBytes(256);

    /// <summary>Throws <see cref="ConfigurationException" /> when counts or byte caps are not greater than zero.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfLessThan(MaxFileCount, 1);
        ArgumentHelpers.ThrowIfLessThan(MaxTotalUncompressedBytes, 1);
    }
}
