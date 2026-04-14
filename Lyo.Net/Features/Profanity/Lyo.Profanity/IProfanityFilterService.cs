using Lyo.Common.Records;
using Lyo.Profanity.Models;

namespace Lyo.Profanity;

/// <summary>Service interface for filtering profanity from text.</summary>
public interface IProfanityFilterService
{
    /// <summary>Filters the input text, replacing detected profanity according to the configured strategy.</summary>
    /// <param name="input">The text to filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The filter result containing the filtered text and match information.</returns>
    ProfanityFilterResult Filter(string? input, CancellationToken ct = default);

    /// <summary>Filters the input text using the specified language's profanity word list.</summary>
    /// <param name="input">The text to filter.</param>
    /// <param name="language">The language for profanity detection. Uses the word list configured for this language via WordsByLanguage or the default source.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The filter result containing the filtered text and match information.</returns>
    ProfanityFilterResult Filter(string? input, LanguageCodeInfo language, CancellationToken ct = default);

    /// <summary>Filters the input text asynchronously.</summary>
    /// <param name="input">The text to filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The filter result containing the filtered text and match information.</returns>
    Task<ProfanityFilterResult> FilterAsync(string? input, CancellationToken ct = default);

    /// <summary>Filters the input text asynchronously using the specified language's profanity word list.</summary>
    /// <param name="input">The text to filter.</param>
    /// <param name="language">The language for profanity detection.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The filter result containing the filtered text and match information.</returns>
    Task<ProfanityFilterResult> FilterAsync(string? input, LanguageCodeInfo language, CancellationToken ct = default);

    /// <summary>Checks whether the input contains any profanity without performing replacement.</summary>
    /// <param name="input">The text to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if profanity was detected.</returns>
    bool ContainsProfanity(string? input, CancellationToken ct = default);

    /// <summary>Checks whether the input contains any profanity using the specified language's word list.</summary>
    /// <param name="input">The text to check.</param>
    /// <param name="language">The language for profanity detection.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if profanity was detected.</returns>
    bool ContainsProfanity(string? input, LanguageCodeInfo language, CancellationToken ct = default);

    /// <summary>Checks whether the input contains any profanity asynchronously.</summary>
    /// <param name="input">The text to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if profanity was detected.</returns>
    Task<bool> ContainsProfanityAsync(string? input, CancellationToken ct = default);

    /// <summary>Checks whether the input contains any profanity asynchronously using the specified language's word list.</summary>
    /// <param name="input">The text to check.</param>
    /// <param name="language">The language for profanity detection.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if profanity was detected.</returns>
    Task<bool> ContainsProfanityAsync(string? input, LanguageCodeInfo language, CancellationToken ct = default);

    /// <summary>Reloads the profanity word list from the configured source (e.g. file). No-op if AllowRefresh is false or source does not support refresh.</summary>
    /// <param name="ct">Cancellation token.</param>
    void RefreshWords(CancellationToken ct = default);

    /// <summary>Reloads the profanity word list asynchronously.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task RefreshWordsAsync(CancellationToken ct = default);
}