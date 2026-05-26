# Lyo.Tts.Typecast.Web.Components

Blazor (MudBlazor) workbench component for exercising [`Lyo.Tts.Typecast`](../Lyo.Tts.Typecast/README.md)
interactively from a host application.

## Components

- `TtsWorkbench` — workbench panel for the Typecast provider:
    - Voice / model / audio-format selectors (model defaults from `TypecastModel.SsfmV30` / `SsfmV21`),
      optional seed, and a tag chip view of the selected voice.
    - Multi-line text input.
    - **Synthesize** / **Test Connection** actions backed by the resolved `TypecastTtsService` (and its
      bound `TypecastOptions` for defaults). The component requires `TypecastTtsService`,
      `TypecastOptions`, `IJsInterop`, and `ISnackbar` in DI, registered via
      [`Lyo.Tts.Typecast`](../Lyo.Tts.Typecast/README.md) and [`Lyo.Typecast.Client`](../../../Integration/Typecast/Lyo.Typecast.Client/README.md).
    - Renders the returned audio bytes inline for browser playback.

## Target framework

`net10.0`

## Related projects

- [`Lyo.Tts.Typecast`](../Lyo.Tts.Typecast/README.md)
- [`Lyo.Tts`](../Lyo.Tts/README.md) / [`Lyo.Tts.Models`](../Lyo.Tts.Models/README.md)
- [`Lyo.Typecast.Client`](../../../Integration/Typecast/Lyo.Typecast.Client/README.md)
- [`Lyo.Web.Components`](../../../Integration/Web/Lyo.Web.Components/README.md)
