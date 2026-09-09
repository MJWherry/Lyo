using Lyo.Common.Metadata.Records;
using Lyo.Translation.Models;

namespace Lyo.Translation;

/// <summary>Contract for translating text from one language into another.</summary>
public interface ITranslationService
{
    /// <summary>Translates <paramref name="text" /> into <paramref name="targetLanguageCode" />.</summary>
    /// <param name="text">Source string to translate.</param>
    /// <param name="targetLanguageCode">Language to translate into.</param>
    /// <param name="sourceLanguage">Source language, if known. When omitted the service tries to detect it.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>Outcome that includes the translated text when successful.</returns>
    Task<TranslationResult> TranslateAsync(string text, LanguageCodeInfo targetLanguageCode, LanguageCodeInfo? sourceLanguage = null, CancellationToken ct = default);

    /// <summary>Translates using a populated request object.</summary>
    /// <param name="request">Request holding the text and language choices.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>Outcome that includes the translated text when successful.</returns>
    Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken ct = default);

    /// <summary>Translates many texts in one bulk call.</summary>
    /// <param name="requests">Requests to process together.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>One result per request.</returns>
    Task<IReadOnlyList<TranslationResult>> TranslateBulkAsync(IEnumerable<TranslationRequest> requests, CancellationToken ct = default);

    /// <summary>Infers the language of <paramref name="text" />.</summary>
    /// <param name="text">Sample whose language should be identified.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>Detected language code, or Unknown when detection fails.</returns>
    Task<LanguageCodeInfo> DetectLanguageAsync(string text, CancellationToken ct = default);

    /// <summary>Checks that the translation provider can be reached.</summary>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>True when the provider answers; otherwise false.</returns>
    Task<bool> TestConnectionAsync(CancellationToken ct = default);
}