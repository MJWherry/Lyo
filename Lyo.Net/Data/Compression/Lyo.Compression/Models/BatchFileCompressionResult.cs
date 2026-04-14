using System.Diagnostics;

namespace Lyo.Compression.Models;

[DebuggerDisplay("{ToString()}")]
public sealed record BatchFileCompressionResult(IReadOnlyList<FileCompressionInfo> SuccessfulFiles, IReadOnlyList<FailedFileOperation> FailedFiles)
{
    public int TotalFiles => SuccessfulFiles.Count + FailedFiles.Count;

    public int SuccessfulFilesCount => SuccessfulFiles.Count;

    public int FailedFilesCount => FailedFiles.Count;

    public long TotalOriginalSize => SuccessfulFiles.Sum(f => f.InputSize);

    public long TotalCompressedSize => SuccessfulFiles.Sum(f => f.OutputSize);

    public long TotalTimeMs => SuccessfulFiles.Sum(f => f.TimeMs);

    public double AverageCompressionRatio => TotalOriginalSize > 0 ? (double)TotalCompressedSize / TotalOriginalSize : 0;

    public double TotalSpaceSavedPercent => (1 - AverageCompressionRatio) * 100;

    public override string ToString()
        => "TotalFiles={TotalFiles} Successful={SuccessfulFilesCount} Failed={FailedFilesCount} " + $"OriginalSize={TotalOriginalSize} CompressedSize={TotalCompressedSize} " +
            $"AvgRatio={AverageCompressionRatio:P2} TotalSaved={TotalSpaceSavedPercent:P2}% Time={TotalTimeMs}ms";
}