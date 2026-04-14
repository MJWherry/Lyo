namespace Lyo.Tts.Models;

/// <summary>Outcome of a provider-agnostic TTS synthesis (used when a single backend is registered).</summary>
public readonly record struct TtsSynthesisResult(bool IsSuccess, byte[]? AudioData, string? ErrorMessage);