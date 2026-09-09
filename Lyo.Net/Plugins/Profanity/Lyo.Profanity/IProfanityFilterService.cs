using Lyo.Common.Metadata.Records;
using Lyo.Profanity.Models;

namespace Lyo.Profanity;

/// <summary>Contract for filtering profanity from text.</summary>
public interface IProfanityFilterService
{
    /// <summary>Runs the filter on the input, replacing detected profanity according to the configured strategy.</summary>
    /// <param name="input">Input run through the filter.</param>
    /// <param name="ct">Token that can abort the call.</param>
    /// <returns>Filter result with the rewritten text and match details.</returns>
    ProfanityFilterResult Filter(string? input, CancellationToken ct = default);

    /// <summary>Runs the filter on the input using the specified language's profanity word list.</summary>
    /// <param name="input">Input run through the filter.</param>
    /// <param name="language">Language used for detection; the WordsByLanguage list or the default source supplies the words.</param>
    /// <param name="ct">Token that can abort the call.</param>
    /// <returns>Filter result with the rewritten text and match details.</returns>
    ProfanityFilterResult Filter(string? input, LanguageCodeInfo language, CancellationToken ct = default);

    /// <summary>Runs the filter on the input asynchronously.</summary>
    /// <param name="input">Input run through the filter.</param>
    /// <param name="ct">Token that can abort the call.</param>
    /// <returns>Filter result with the rewritten text and match details.</returns>
    Task<ProfanityFilterResult> FilterAsync(string? input, CancellationToken ct = default);

    /// <summary>Runs the filter asynchronously using the specified language's profanity word list.</summary>
    /// <param name="input">Input run through the filter.</param>
    /// <param name="language">Language whose word list is used.</param>
    /// <param name="ct">Token that can abort the call.</param>
    /// <returns>Filter result with the rewritten text and match details.</returns>
    Task<ProfanityFilterResult> FilterAsync(string? input, LanguageCodeInfo language, CancellationToken ct = default);

    /// <summary>Reports whether the input has profanity without performing replacement.</summary>
    /// <param name="input">Input scanned for profanity.</param>
    /// <param name="ct">Token that can abort the call.</param>
    /// <returns>True when profanity was found.</returns>
    bool ContainsProfanity(string? input, CancellationToken ct = default);

    /// <summary>Reports whether the input has profanity using the specified language's word list.</summary>
    /// <param name="input">Input scanned for profanity.</param>
    /// <param name="language">Language whose word list is used.</param>
    /// <param name="ct">Token that can abort the call.</param>
    /// <returns>True when profanity was found.</returns>
    bool ContainsProfanity(string? input, LanguageCodeInfo language, CancellationToken ct = default);

    /// <summary>Reports whether the input has profanity asynchronously.</summary>
    /// <param name="input">Input scanned for profanity.</param>
    /// <param name="ct">Token that can abort the call.</param>
    /// <returns>True when profanity was found.</returns>
    Task<bool> ContainsProfanityAsync(string? input, CancellationToken ct = default);

    /// <summary>Reports whether the input has profanity asynchronously using the specified language's word list.</summary>
    /// <param name="input">Input scanned for profanity.</param>
    /// <param name="language">Language whose word list is used.</param>
    /// <param name="ct">Token that can abort the call.</param>
    /// <returns>True when profanity was found.</returns>
    Task<bool> ContainsProfanityAsync(string? input, LanguageCodeInfo language, CancellationToken ct = default);

    /// <summary>Reloads the profanity word list of the configured source (e.g. file). No-op if AllowRefresh is false or source does not support refresh.</summary>
    /// <param name="ct">Token that can abort the call.</param>
    void RefreshWords(CancellationToken ct = default);

    /// <summary>Reloads the word list asynchronously.</summary>
    /// <param name="ct">Token that can abort the call.</param>
    Task RefreshWordsAsync(CancellationToken ct = default);
}