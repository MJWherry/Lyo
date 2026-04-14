using Lyo.Common;
using Lyo.Common.Records;

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

    /// <summary>Creates a successful TranslationResult with translated text.</summary>
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

    /// <summary>Creates a failed TranslationResult from an exception.</summary>
    public static TranslationResult FromException(Exception exception, TranslationRequest request, TimeSpan elapsedTime, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, request, [error]);
    }

    /// <summary>Creates a failed TranslationResult with a custom error message.</summary>
    public static TranslationResult FromError(string errorMessage, string errorCode, TranslationRequest request, TimeSpan elapsedTime, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, request, [error]);
    }
}