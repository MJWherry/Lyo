using System.Diagnostics;

namespace Lyo.Tts.Models;

/// <summary>Small success/failure snapshot for <see cref="Lyo.Tts.ITtsService" /> facades (no typed request or <see cref="Lyo.Result.Result{T}" /> graph).</summary>
/// <param name="IsSuccess">Whether audio was produced.</param>
/// <param name="AudioData">Raw audio bytes on success; otherwise null.</param>
/// <param name="ErrorMessage">Readable error when <paramref name="IsSuccess" /> is false.</param>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct TtsSynthesisResult(bool IsSuccess, byte[]? AudioData, string? ErrorMessage)
{
    /// <summary>Success flag and audio length. Audio bytes are omitted.</summary>
    public override string ToString() => $"IsSuccess={IsSuccess} AudioBytes={AudioData?.Length ?? 0} Error={ErrorMessage}";
}