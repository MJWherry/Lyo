using Lyo.Common.Records;
using Lyo.Result;

namespace Lyo.Translation.Models;

/// <summary>Result of a translation operation with translation-specific properties.</summary>
public sealed record TranslationResult : Result<TranslationRequest>
{
    /// <summary>The translated text.</summary>
    public string? TranslatedText { get; init; }

    /// <summary>The detected source language (if auto-detected).</summary>
    public LanguageCodeInfo? DetectedSourceLanguage { get; init; }

    /// <summary>The request ID from the translation provider.</summary>
    public string? RequestId { get; init; }

    /// <summary>The message describing the result.</summary>
    public string? Message { get; init; }

    private TranslationResult(bool isSuccess, TranslationRequest? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Creates a successful translation result.</summary>
    /// <param name="request">Original request payload.</param>
    /// <param name="translatedText">Translated string from the provider.</param>
    /// <param name="elapsedTime">Observed duration.</param>
    /// <param name="detectedSourceLanguage">Filled when the engine inferred source language.</param>
    /// <param name="requestId">Optional provider correlation id.</param>
    /// <param name="message">Optional informational message.</param>
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

    /// <summary>Creates a failure result wrapping an exception.</summary>
    /// <param name="exception">Cause of failure.</param>
    /// <param name="request">Associated request.</param>
    /// <param name="elapsedTime">Time spent before failing.</param>
    /// <param name="errorCode">Optional stable code (see <see cref="TranslationErrorCodes"/>).</param>
    public static TranslationResult FromException(Exception exception, TranslationRequest request, TimeSpan elapsedTime, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, request, [error]);
    }

    /// <summary>Creates a failure result with explicit messaging.</summary>
    /// <param name="errorMessage">Description suitable for logs or callers.</param>
    /// <param name="errorCode">Stable error code.</param>
    /// <param name="request">Associated request.</param>
    /// <param name="elapsedTime">Time spent before failing.</param>
    /// <param name="exception">Optional inner exception.</param>
    public static TranslationResult FromError(string errorMessage, string errorCode, TranslationRequest request, TimeSpan elapsedTime, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, request, [error]);
    }
}