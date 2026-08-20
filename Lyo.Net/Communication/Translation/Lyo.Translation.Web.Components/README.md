# Lyo.Translation.Web.Components

Blazor (MudBlazor) workbench for the configured [`Lyo.Translation`](../Lyo.Translation/README.md) implementation.

## Components

- `TranslationWorkbench` works with any registered `ITranslationService`.
- Source and target language selectors keyed on `LanguageCodeInfo.Bcp47`, with an auto-detect option for the source.
- Multi-line input for freeform text.
- **Translate**, **Detect Language**, and **Test Connection** call the resolved `ITranslationService`.
- A side panel shows the detected source language, the chosen target, the translated text, and any errors. Transient notices go through `ISnackbar`.

## Target framework

`net10.0`

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Translation` (direct, lyo)
- `MudBlazor` `9.3` (direct, third-party)
- `Lyo.Common` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)