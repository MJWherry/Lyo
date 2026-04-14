using Lyo.Common;

namespace Lyo.Tts.Models;

/// <summary>Result of a TTS synthesis operation with TTS-specific properties.</summary>
/// <typeparam name="TRequest">The type of TTS request.</typeparam>
public sealed record TtsResult<TRequest> : Result<TRequest>
    where TRequest : TtsRequest
{
    /// <summary>The synthesized audio data.</summary>
    public byte[]? AudioData { get; init; }

    /// <summary>The request ID from the TTS provider.</summary>
    public string? RequestId { get; init; }

    /// <summary>The message describing the result.</summary>
    public string? Message { get; init; }

    /// <summary>The size of the audio data in bytes.</summary>
    public int? AudioSize { get; init; }

    private TtsResult(bool isSuccess, TRequest? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Creates a successful TtsResult with audio data.</summary>
    public static TtsResult<TRequest> FromSuccess(TRequest request, byte[] audioData, TimeSpan elapsedTime, string? requestId = null, string? message = null)
        => new(true, request) {
            AudioData = audioData,
            RequestId = requestId,
            Message = message,
            AudioSize = audioData.Length
        };

    /// <summary>Creates a failed TtsResult from an exception.</summary>
    public static TtsResult<TRequest> FromException(Exception exception, TRequest request, TimeSpan elapsedTime, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, request, [error]);
    }

    /// <summary>Creates a failed TtsResult with a custom error message.</summary>
    public static TtsResult<TRequest> FromError(string errorMessage, string errorCode, TRequest request, TimeSpan elapsedTime, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, request, [error]);
    }
}