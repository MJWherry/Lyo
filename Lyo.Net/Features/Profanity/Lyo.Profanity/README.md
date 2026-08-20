# Lyo.Profanity

File-based profanity filter. Detects and replaces profane words. Multiple languages, regex patterns, plain word lists, and configurable replacement strategies.

## Features

- **Word sources.** JSON file, HTTP URL, or per-language `WordsByLanguage`
- **Format support.** Structured JSON (`[{ id, match, tags, severity, exceptions }]`), plain array `["word1", "word2"]`, or plain newline-separated text (one word per line)
- **Replacement strategies.** `Remove`, `ReplaceWithChar`, `ReplaceWithWord`, `Mask`, `PreserveBoundary`, `DetectOnly`
- **Per-language.** Load different word lists by BCP 47 / ISO 639-1 / ISO 639-3

## Examples

### Usage

```csharp
services.AddProfanityFilterService(options =>
{
    options.WordsFilePath = "profanity-words.json";
    options.ReplacementStrategy = ProfanityReplacementStrategy.Mask;
});

// Or bind from configuration
services.AddProfanityFilterServiceFromConfiguration(configuration, "ProfanityFilter");

// In a service
var result = await _profanityFilter.FilterAsync("some text with bad word", ct);
// result.FilteredText, result.HasProfanity, result.Matches
```

### Configuration (appsettings.json)

```json
{
  "ProfanityFilter": {
    "WordsFilePath": "profanity-en.json",
    "WordsUrl": "https://example.com/words.json",
    "ReplacementStrategy": "Mask",
    "Language": "en-US",
    "AllowRefresh": true,
    "WordsByLanguage": {
      "en": { "WordsFilePath": "en.json" },
      "es": { "WordsFilePath": "es.json" }
    }
  }
}
```

## DI registration

`Lyo.Profanity.Extensions` exposes three entry points on `IServiceCollection`. Pick exactly one when wiring up the host:

| Entry point | Behaviour |
| ------------------------------------------------------------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `services.AddProfanityFilterService()` | Registers a default `FileProfanityFilterOptions` and `FileProfanityFilterService` (resolved as both itself and `IProfanityFilterService`). Useful in tests that only need the methods. |
| `services.AddProfanityFilterService(Action<FileProfanityFilterOptions> configure)` | Same registration, with an inline options callback. |
| `services.AddProfanityFilterServiceFromConfiguration(IConfiguration configuration, string sectionName = FileProfanityFilterOptions.SectionName)` | Same registration, binding options from the configuration section (default `"ProfanityFilter"`). |

`FileProfanityFilterService` resolves an optional `ILogger<FileProfanityFilterService>`, an optional `IMetrics` (used only when `Options.EnableMetrics == true`), and an
optional `HttpClient` (used when `WordsUrl` is configured).

## `IProfanityFilterService` methods

- `Filter(string? input, CancellationToken)` / `Filter(string? input, LanguageCodeInfo language, CancellationToken)` synchronous filtering; returns `ProfanityFilterResult` (`FilteredText`, `HasProfanity`, `Matches`).
- `FilterAsync(string? input, CancellationToken)` / `FilterAsync(string? input, LanguageCodeInfo language, CancellationToken)` same as above, async.
- `ContainsProfanity(string? input, CancellationToken)` / `ContainsProfanity(string? input, LanguageCodeInfo language, CancellationToken)` fast boolean check, no replacement.
- `ContainsProfanityAsync(string? input, CancellationToken)` / `ContainsProfanityAsync(string? input, LanguageCodeInfo language, CancellationToken)` async variants.
- `RefreshWords(CancellationToken)` / `RefreshWordsAsync(CancellationToken)` reload from the configured file/URL. No-op when `Options.AllowRefresh` is false or the source doesn't support refresh.

## Word list formats

- **Plain JSON array.** `["word1", "word2"]` becomes default entries (`id == match == word`, `tags = []`, `severity = 1`, `exceptions = []`).
- **Structured JSON.** `[{ "id": "x", "match": "regex|word", "tags": [], "severity": 1, "exceptions": [] }]`. `match` is compiled as a `Regex` and cached.
- **Plain newline-separated text.** One word per line, same defaults as the plain JSON array.

## Replacement strategies

| Strategy | Example (input → output) |
| ---------------- | --------------------------- |
| Remove | "bad" → "" |
| ReplaceWithChar | "bad" → "***" |
| ReplaceWithWord | "bad" → "***" |
| Mask | "bad" → "***" |
| PreserveBoundary | "bad" → "b*d" |
| DetectOnly | No replacement; only detect |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (direct, microsoft)
- `System.Text.Json` `10.0.5` (direct, microsoft, netstandard2.0)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)