using System.Diagnostics;

namespace Lyo.Compression.Models;

[DebuggerDisplay("{ToString()}")]
public sealed record BatchFileDecompressionResult(IReadOnlyList<FileDecompressionInfo> SuccessfulFiles, IReadOnlyList<FailedFileOperation> FailedFiles)
{
    public int TotalFiles => SuccessfulFiles.Count + FailedFiles.Count;

    public int SuccessfulFilesCount => SuccessfulFiles.Count;

    public int FailedFilesCount => FailedFiles.Count;

    public long TotalCompressedSize => SuccessfulFiles.Sum(f => f.InputSize);

    public long TotalDecompressedSize => SuccessfulFiles.Sum(f => f.OutputSize);

    public long TotalTimeMs => SuccessfulFiles.Sum(f => f.TimeMs);

    public double AverageExpansionRatio => TotalCompressedSize > 0 ? (double)TotalDecompressedSize / TotalCompressedSize : 0;

    public double TotalSizeIncreasePercent => (AverageExpansionRatio - 1) * 100;

    public override string ToString()
        => "TotalFiles={TotalFiles} Successful={SuccessfulFilesCount} Failed={FailedFilesCount} " +
            $"CompressedSize={TotalCompressedSize} DecompressedSize={TotalDecompressedSize} " +
            $"AvgRatio={AverageExpansionRatio:P2} TotalIncrease={TotalSizeIncreasePercent:P2}% Time={TotalTimeMs}ms";
}