using System.Diagnostics;

namespace Lyo.Images.Models;

/// <summary>Outcome of a batch image run.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public record BatchImageResult(int TotalProcessed, int Successful, int Failed, TimeSpan ElapsedTime, IReadOnlyList<ImageProcessResult> Results)
{
    public override string ToString() => $"Batch Image Processing: {Successful}/{TotalProcessed} successful in {ElapsedTime:g}";
}

/// <summary>Outcome of one image operation.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public record ImageProcessResult(bool IsSuccess, string? ErrorMessage, Exception? Exception, TimeSpan ElapsedTime)
{
    public override string ToString() => IsSuccess ? $"Success in {ElapsedTime:g}" : $"Failed: {ErrorMessage} - {Exception?.Message}";
}