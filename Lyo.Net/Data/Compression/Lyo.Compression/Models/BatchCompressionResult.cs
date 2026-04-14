using System.Diagnostics;

namespace Lyo.Compression.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record BatchCompressionResult(IReadOnlyList<CompressionFileInfo> SuccessfulFiles, IReadOnlyList<FailedFileOperation> FailedFiles)
{
    public int TotalFiles => SuccessfulFiles.Count + FailedFiles.Count;

    public int SuccessfulFilesCount => SuccessfulFiles.Count;

    public int FailedFilesCount => FailedFiles.Count;

    public long TotalOriginalSize => SuccessfulFiles.Sum(f => f.UncompressedSize);

    public long TotalCompressedSize => SuccessfulFiles.Sum(f => f.CompressedSize);

    public long TotalTimeMs => SuccessfulFiles.Sum(f => f.TimeMs);

    public double AverageCompressionRatio => TotalOriginalSize > 0 ? (double)TotalCompressedSize / TotalOriginalSize : 0;

    public double TotalSpaceSavedPercent => (1 - AverageCompressionRatio) * 100;

    public override string ToString()
        => $"BatchCompressionResult: TotalFiles: {TotalFiles}, SuccessfulFiles: {SuccessfulFilesCount}, FailedFiles: {FailedFilesCount}, TotalOriginalSize: {TotalOriginalSize}, TotalCompressedSize: {TotalCompressedSize}, AverageCompressionRatio: {AverageCompressionRatio:P2}, TotalSpaceSavedPercent: {TotalSpaceSavedPercent:N2}%, TotalTimeMs: {TotalTimeMs}";
}