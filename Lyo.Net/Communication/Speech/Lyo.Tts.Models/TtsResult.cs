using Lyo.Result;

namespace Lyo.Tts.Models;

/// <summary>Outcome of a synthesis, including fields unique to TTS.</summary>
/// <typeparam name="TRequest">TTS request type.</typeparam>
public sealed record TtsResult<TRequest> : Result<TRequest>
    where TRequest : TtsRequest
{
    /// <summary>Synthesized audio bytes.</summary>
    public byte[]? AudioData { get; init; }

    /// <summary>Correlation id assigned by the TTS provider.</summary>
    public string? RequestId { get; init; }

    /// <summary>Human-readable note about the outcome.</summary>
    public string? Message { get; init; }

    /// <summary>Audio size, in bytes.</summary>
    public int? AudioSize { get; init; }

    private TtsResult(bool isSuccess, TRequest? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Builds a successful outcome that holds synthesized audio.</summary>
    /// <param name="request">Request that was honored (stored on the result).</param>
    /// <param name="audioData">Non-empty audio payload.</param>
    /// <param name="elapsedTime">Measured duration (reserved for later use and provider logging).</param>
    /// <param name="requestId">Optional provider correlation id.</param>
    /// <param name="message">Optional extra note.</param>
    public static TtsResult<TRequest> FromSuccess(TRequest request, byte[] audioData, TimeSpan elapsedTime, string? requestId = null, string? message = null)
        => new(true, request) {
            AudioData = audioData,
            RequestId = requestId,
            Message = message,
            AudioSize = audioData.Length
        };

    /// <summary>Builds a failure outcome from an exception.</summary>
    /// <param name="exception">Error that stopped the work.</param>
    /// <param name="request">Request that failed.</param>
    /// <param name="elapsedTime">Time elapsed before the failure.</param>
    /// <param name="errorCode">Optional stable code (see <see cref="Lyo.Tts.TtsErrorCodes" />).</param>
    public static TtsResult<TRequest> FromException(Exception exception, TRequest request, TimeSpan elapsedTime, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, request, [error]);
    }

    /// <summary>Builds a failure outcome with a supplied message.</summary>
    /// <param name="errorMessage">Description for callers or the provider.</param>
    /// <param name="errorCode">Stable error code.</param>
    /// <param name="request">Request that failed.</param>
    /// <param name="elapsedTime">Time elapsed before the failure.</param>
    /// <param name="exception">Optional inner exception.</param>
    public static TtsResult<TRequest> FromError(string errorMessage, string errorCode, TRequest request, TimeSpan elapsedTime, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, request, [error]);
    }
}