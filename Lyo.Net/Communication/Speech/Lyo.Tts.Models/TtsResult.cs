using Lyo.Result;

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

    /// <summary>Creates a successful result that carries synthesized audio.</summary>
    /// <param name="request">The request that was honored (stored on the result).</param>
    /// <param name="audioData">Non-empty audio payload.</param>
    /// <param name="elapsedTime">Observed duration (reserved for future use and provider logging).</param>
    /// <param name="requestId">Optional provider correlation id.</param>
    /// <param name="message">Optional informational message.</param>
    public static TtsResult<TRequest> FromSuccess(TRequest request, byte[] audioData, TimeSpan elapsedTime, string? requestId = null, string? message = null)
        => new(true, request) {
            AudioData = audioData,
            RequestId = requestId,
            Message = message,
            AudioSize = audioData.Length
        };

    /// <summary>Creates a failure result from an exception.</summary>
    /// <param name="exception">The error to wrap.</param>
    /// <param name="request">The request associated with the failure.</param>
    /// <param name="elapsedTime">Observed duration before failure.</param>
    /// <param name="errorCode">Optional stable code (see <see cref="Lyo.Tts.TtsErrorCodes" />).</param>
    public static TtsResult<TRequest> FromException(Exception exception, TRequest request, TimeSpan elapsedTime, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, request, [error]);
    }

    /// <summary>Creates a failure result with an explicit message.</summary>
    /// <param name="errorMessage">User- or provider-facing description.</param>
    /// <param name="errorCode">Stable error code.</param>
    /// <param name="request">The request associated with the failure.</param>
    /// <param name="elapsedTime">Observed duration before failure.</param>
    /// <param name="exception">Optional inner exception.</param>
    public static TtsResult<TRequest> FromError(string errorMessage, string errorCode, TRequest request, TimeSpan elapsedTime, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, request, [error]);
    }
}