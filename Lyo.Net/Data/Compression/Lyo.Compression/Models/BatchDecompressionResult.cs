using System.Diagnostics;

namespace Lyo.Compression.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record BatchDecompressionResult(IReadOnlyList<DecompressionFileInfo> SuccessfulFiles, IReadOnlyList<FailedFileOperation> FailedFiles)
{
    public int TotalFiles => SuccessfulFiles.Count + FailedFiles.Count;

    public int SuccessfulFilesCount => SuccessfulFiles.Count;

    public int FailedFilesCount => FailedFiles.Count;

    public long TotalCompressedSize => SuccessfulFiles.Sum(f => f.CompressedSize);

    public long TotalDecompressedSize => SuccessfulFiles.Sum(f => f.DecompressedSize);

    public long TotalTimeMs => SuccessfulFiles.Sum(f => f.TimeMs);

    public double AverageExpansionRatio => TotalCompressedSize > 0 ? (double)TotalDecompressedSize / TotalCompressedSize : 0;

    public double TotalSizeIncreasePercent => (AverageExpansionRatio - 1) * 100;

    public override string ToString()
        => $"BatchDecompressionResult: TotalFiles: {TotalFiles}, SuccessfulFiles: {SuccessfulFilesCount}, FailedFiles: {FailedFilesCount}, TotalCompressedSize: {TotalCompressedSize}, TotalDecompressedSize: {TotalDecompressedSize}, AverageExpansionRatio: {AverageExpansionRatio:P2}, TotalSizeIncreasePercent: {TotalSizeIncreasePercent:N2}%, TotalTimeMs: {TotalTimeMs}";
}