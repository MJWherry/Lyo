namespace Lyo.Tts.Models;

/// <summary>Small success/failure snapshot for <see cref="Lyo.Tts.ITtsService" /> facades (no typed request or <see cref="Lyo.Result.Result{T}" /> graph).</summary>
/// <param name="IsSuccess">Whether audio was produced.</param>
/// <param name="AudioData">Raw audio bytes on success; otherwise null.</param>
/// <param name="ErrorMessage">Readable error when <paramref name="IsSuccess" /> is false.</param>
public readonly record struct TtsSynthesisResult(bool IsSuccess, byte[]? AudioData, string? ErrorMessage);