# Lyo.Translation.Web.Components

Blazor (MudBlazor) workbench component for exercising the configured [`Lyo.Translation`](../Lyo.Translation/README.md)
implementation interactively from a host application.

## Components

- `TranslationWorkbench` — provider-neutral workbench panel for any registered `ITranslationService`:
    - Source / target language selectors keyed on `LanguageCodeInfo.Bcp47`, with an "auto detect" option
      for the source.
    - Multi-line input area for freeform text.
    - **Translate**, **Detect Language**, and **Test Connection** actions backed by the resolved
      `ITranslationService`.
    - A side panel that renders the detected source language, the chosen target, the translated text, and
      any errors (with `ISnackbar` feedback for transient notifications).

The component depends on `ITranslationService` and `ISnackbar` from DI; pair it with a Blazor host that
has MudBlazor configured and a translation provider package (e.g. [`Lyo.Translation.Google`](../Lyo.Translation.Google/README.md)
or [`Lyo.Translation.Aws`](../Lyo.Translation.Aws/README.md)) registered.

## Target framework

`net10.0`

## Related projects

- [`Lyo.Translation`](../Lyo.Translation/README.md)
- [`Lyo.Translation.Google`](../Lyo.Translation.Google/README.md)
- [`Lyo.Translation.Aws`](../Lyo.Translation.Aws/README.md)
- [`Lyo.Web.Components`](../../../Integration/Web/Lyo.Web.Components/README.md)
