# Lyo.Profanity

File-based profanity filter service that detects and replaces profane words in text. Supports multiple languages, regex patterns, plain word lists, and configurable replacement
strategies.

## Features

- **Word sources** – JSON file, HTTP URL, or per-language `WordsByLanguage`
- **Format support** – Structured JSON (`{ id, match, tags, severity, exceptions }`) or plain array `["word1", "word2"]`
- **Replacement strategies** – Remove, ReplaceWithChar, ReplaceWithWord, Mask, PreserveBoundary, DetectOnly
- **Per-language** – Load different word lists by BCP 47 / ISO 639-1 / ISO 639-3

## Usage

```csharp
services.AddProfanityFilterService(options =>
{
    options.WordsFilePath = "profanity-words.json";
    options.ReplacementStrategy = ProfanityReplacementStrategy.Mask;
});

// Or from configuration
services.AddProfanityFilterService(configuration, "ProfanityFilter");

// In a service
var result = await _profanityFilter.FilterAsync("some text with bad word", ct);
// result.FilteredText, result.HasProfanity, result.Matches
```

## Configuration (appsettings.json)

```json
{
  "ProfanityFilter": {
    "WordsFilePath": "profanity-en.json",
    "WordsUrl": "https://example.com/words.json",
    "ReplacementStrategy": "Mask",
    "Language": "en-US",
    "WordsByLanguage": {
      "en": { "WordsFilePath": "en.json" },
      "es": { "WordsFilePath": "es.json" }
    }
  }
}
```

## Word list formats

- **Plain array**: `["word1", "word2"]` → default entries
- **Structured**: `[{ "id": "x", "match": "regex|word", "tags": [], "severity": 1, "exceptions": [] }]`

## Replacement strategies

| Strategy         | Example (input → output)    |
|------------------|-----------------------------|
| Remove           | "bad" → ""                  |
| ReplaceWithChar  | "bad" → "***"               |
| ReplaceWithWord  | "bad" → "***"               |
| Mask             | "bad" → "***"               |
| PreserveBoundary | "bad" → "b*d"               |
| DetectOnly       | No replacement; only detect |

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Profanity.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Logging.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Options` | `[10,)` |
| `System.Text.Json` | `[10,)` |

### Project references

- `Lyo.Common`
- `Lyo.Exceptions`
- `Lyo.Metrics`

## Public API (generated)

Top-level `public` types in `*.cs` (*10*). Nested types and file-scoped namespaces may omit some entries.

- `Constants`
- `Extensions`
- `FileProfanityFilterOptions`
- `FileProfanityFilterService`
- `IProfanityFilterService`
- `IsExternalInit`
- `LanguageWordSourceConfig`
- `Metrics`
- `ProfanityFilterOptions`
- `ProfanityReplacementStrategy`

<!-- LYO_README_SYNC:END -->

