using Lyo.Common.Records;
using Lyo.Exceptions;

namespace Lyo.FileStorage.Models;

/// <summary>Caps for <see cref="IFileStorageArchiveService" /> zip downloads. Bind from configuration section <see cref="SectionName" />.</summary>
public sealed class FileStorageArchiveOptions
{
    /// <summary>Configuration section name. Default: <c>FileStorageArchive</c>.</summary>
    public const string SectionName = "FileStorageArchive";

    /// <summary>Maximum number of files in one archive. Default: 100.</summary>
    public int MaxFileCount { get; set; } = 100;

    /// <summary>Maximum sum of <c>OriginalFileSize</c> across entries, checked before any download. Default: 256 MiB.</summary>
    public long MaxTotalUncompressedBytes { get; set; } = FileSizeUnitInfo.Megabyte.ConvertToBytes(256);

    /// <summary>Throws <see cref="ConfigurationException" /> when counts or byte caps are not positive.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfLessThan(MaxFileCount, 1);
        ArgumentHelpers.ThrowIfLessThan(MaxTotalUncompressedBytes, 1);
    }
}
