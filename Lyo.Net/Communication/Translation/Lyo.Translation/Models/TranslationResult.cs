using Lyo.Common.Metadata.Records;
using Lyo.Result;

namespace Lyo.Translation.Models;

/// <summary>Outcome of a translation, including fields unique to that operation.</summary>
public sealed record TranslationResult : Result<TranslationRequest>
{
    /// <summary>Text returned by the provider.</summary>
    public string? TranslatedText { get; init; }

    /// <summary>Source language inferred by the engine, when auto-detection ran.</summary>
    public LanguageCodeInfo? DetectedSourceLanguage { get; init; }

    /// <summary>Correlation id assigned by the translation provider.</summary>
    public string? RequestId { get; init; }

    /// <summary>Human-readable note about the outcome.</summary>
    public string? Message { get; init; }

    private TranslationResult(bool isSuccess, TranslationRequest? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Builds a successful translation outcome.</summary>
    /// <param name="request">Request that was sent.</param>
    /// <param name="translatedText">Provider output string.</param>
    /// <param name="elapsedTime">Measured duration.</param>
    /// <param name="detectedSourceLanguage">Set when the engine guessed the source language.</param>
    /// <param name="requestId">Optional provider correlation id.</param>
    /// <param name="message">Optional extra note.</param>
    public static TranslationResult FromSuccess(
        TranslationRequest request,
        string translatedText,
        TimeSpan elapsedTime,
        LanguageCodeInfo? detectedSourceLanguage = null,
        string? requestId = null,
        string? message = null)
        => new(true, request) {
            TranslatedText = translatedText,
            DetectedSourceLanguage = detectedSourceLanguage,
            RequestId = requestId,
            Message = message
        };

    /// <summary>Builds a failure outcome from an exception.</summary>
    /// <param name="exception">Error that stopped the work.</param>
    /// <param name="request">Request that failed.</param>
    /// <param name="elapsedTime">Time elapsed before the failure.</param>
    /// <param name="errorCode">Optional stable code (see <see cref="TranslationErrorCodes" />).</param>
    public static TranslationResult FromException(Exception exception, TranslationRequest request, TimeSpan elapsedTime, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, request, [error]);
    }

    /// <summary>Builds a failure outcome with a supplied message.</summary>
    /// <param name="errorMessage">Text suitable for logs or callers.</param>
    /// <param name="errorCode">Stable error code.</param>
    /// <param name="request">Request that failed.</param>
    /// <param name="elapsedTime">Time elapsed before the failure.</param>
    /// <param name="exception">Optional inner exception.</param>
    public static TranslationResult FromError(string errorMessage, string errorCode, TranslationRequest request, TimeSpan elapsedTime, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, request, [error]);
    }
}