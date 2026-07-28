using System.Diagnostics;

#if NETSTANDARD2_0
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#endif

namespace Lyo.Stt.Models;

[DebuggerDisplay("{ToString(),nq}")]
public record SttResult(
    bool IsSuccess,
    string? Message,
    string? ErrorMessage,
    Exception? Exception,
    TimeSpan ElapsedTime,
    SttRequest? SttRequest = null,
    string? Text = null,
    double? Confidence = null,
    string? RequestId = null,
    int? ErrorCode = null)
{
    public static SttResult Success(string text, TimeSpan elapsed, SttRequest? sttRequest = null, double? confidence = null, string? requestId = null)
        => new(true, null, null, null, elapsed, sttRequest, text, confidence, requestId);

    public static SttResult Success(string text, TimeSpan elapsed, byte[]? audioData = null, string? audioFilePath = null, double? confidence = null, string? requestId = null)
        => new(true, null, null, null, elapsed, new() { AudioData = audioData, AudioFilePath = audioFilePath }, text, confidence, requestId);

    public static SttResult Failure(string errorMessage, Exception exception, TimeSpan elapsed, SttRequest? sttRequest = null, int? errorCode = null)
        => new(false, null, errorMessage, exception, elapsed, sttRequest, null, null, null, errorCode);

    public static SttResult Failure(string errorMessage, Exception exception, TimeSpan elapsed, byte[]? audioData = null, string? audioFilePath = null, int? errorCode = null)
        => new(false, null, errorMessage, exception, elapsed, new() { AudioData = audioData, AudioFilePath = audioFilePath }, null, null, null, errorCode);

    public override string ToString()
    {
        if (IsSuccess) {
            var parts = new List<string> { $"{ElapsedTime:g}" };
            if (!string.IsNullOrWhiteSpace(Text))
                parts.Add($"Text: {Text.Substring(0, Math.Min(Text.Length, 50))}{(Text.Length > 50 ? "..." : "")}");

            if (Confidence.HasValue)
                parts.Add($"Confidence: {Confidence:P2}");

            if (!string.IsNullOrWhiteSpace(RequestId))
                parts.Add($"RequestId: {RequestId}");

            if (!string.IsNullOrWhiteSpace(Message))
                parts.Add(Message!);

            return string.Join(" | ", parts);
        }

        var errorParts = new List<string> { $"{ElapsedTime:g}" };
        if (SttRequest != null) {
            if (SttRequest.AudioData != null)
                errorParts.Add($"AudioData: {SttRequest.AudioData.Length} bytes");

            if (!string.IsNullOrWhiteSpace(SttRequest.AudioFilePath))
                errorParts.Add($"AudioFilePath: {SttRequest.AudioFilePath}");
        }

        if (ErrorCode.HasValue)
            errorParts.Add($"ErrorCode: {ErrorCode}");

        errorParts.Add($"{ErrorMessage} - {Exception?.Message}");
        return string.Join(" | ", errorParts);
    }
}