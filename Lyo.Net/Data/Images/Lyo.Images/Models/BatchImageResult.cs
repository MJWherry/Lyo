using System.Diagnostics;

namespace Lyo.Images.Models;

/// <summary>Represents the result of batch image processing.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public record BatchImageResult(int TotalProcessed, int Successful, int Failed, TimeSpan ElapsedTime, IReadOnlyList<ImageProcessResult> Results)
{
    public override string ToString() => $"Batch Image Processing: {Successful}/{TotalProcessed} successful in {ElapsedTime:g}";
}

/// <summary>Represents the result of a single image processing operation.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public record ImageProcessResult(bool IsSuccess, string? ErrorMessage, Exception? Exception, TimeSpan ElapsedTime)
{
    public override string ToString() => IsSuccess ? $"Success in {ElapsedTime:g}" : $"Failed: {ErrorMessage} - {Exception?.Message}";
}