using Lyo.Common.Records;
using Lyo.Translation.Models;

namespace Lyo.Translation;

/// <summary>Service interface for translating text between languages.</summary>
public interface ITranslationService
{
    /// <summary>Translates text from one language to another.</summary>
    /// <param name="text">The text to translate.</param>
    /// <param name="targetLanguageCode">The target language code.</param>
    /// <param name="sourceLanguage">Optional source language code. If not provided, the service will attempt to detect it.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the translation operation containing translated text.</returns>
    Task<TranslationResult> TranslateAsync(string text, LanguageCodeInfo targetLanguageCode, LanguageCodeInfo? sourceLanguage = null, CancellationToken ct = default);

    /// <summary>Translates text using a request object.</summary>
    /// <param name="request">The translation request containing text and language options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the translation operation containing translated text.</returns>
    Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken ct = default);

    /// <summary>Translates multiple texts in bulk.</summary>
    /// <param name="requests">Collection of translation requests.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of results for each translation operation.</returns>
    Task<IReadOnlyList<TranslationResult>> TranslateBulkAsync(IEnumerable<TranslationRequest> requests, CancellationToken ct = default);

    /// <summary>Detects the language of the provided text.</summary>
    /// <param name="text">The text to detect the language for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The detected language code, or Unknown if detection fails.</returns>
    Task<LanguageCodeInfo> DetectLanguageAsync(string text, CancellationToken ct = default);

    /// <summary>Tests the connection to the translation service provider.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the connection is successful, false otherwise.</returns>
    Task<bool> TestConnectionAsync(CancellationToken ct = default);
}