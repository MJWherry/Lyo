# Lyo.Translation.Web.Components

MudBlazor workbench for whichever [`Lyo.Translation`](../Lyo.Translation/README.md) implementation the host wired.

## Components

- `TranslationWorkbench` talks to whatever `ITranslationService` is registered.
- Source and target language selectors keyed on `LanguageCodeInfo.Bcp47`, with auto-detect available for the source.
- Multi-line field for freeform text.
- **Translate**, **Detect Language**, and **Test Connection** invoke the resolved `ITranslationService`.
- A side panel lists the detected source language, the chosen target, the translated text, and any errors. Transient notices go through `ISnackbar`.

## Supported framework

`net10.0`

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Translation` (direct, lyo)
- `MudBlazor` `9.3` (direct, third-party)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)