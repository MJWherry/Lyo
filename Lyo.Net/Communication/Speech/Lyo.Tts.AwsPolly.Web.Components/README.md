# Lyo.Tts.AwsPolly.Web.Components

Blazor (MudBlazor) workbench component for trying out the [`Lyo.Tts.AwsPolly`](../Lyo.Tts.AwsPolly/README.md)
implementation interactively from a host application.

## Components

- `AwsPollyWorkbench` — voice / audio format / language selector backed by `AwsPollyVoiceId`, a multi-line
  text input, and **Synthesize** / **Test Connection** actions:
    - Resolves `AwsPollyTtsService` and `AwsPollyOptions` from the DI container (so the host must register
      them via [`Lyo.Tts.AwsPolly`](../Lyo.Tts.AwsPolly/README.md) first).
    - Calls `AwsPollyTtsService.SynthesizeAsync(...)` with a populated `AwsPollyTtsRequest`, surfacing
      status text via `MudAlert` and feedback through `ISnackbar`.
    - Renders the returned audio bytes inline via `IJsInterop` for browser playback.

The component is part of the internal workbench layer; pair it with a hosting Blazor app that has
MudBlazor configured and the Polly DI surface registered.

## Target framework

`net10.0`

## Related projects

- [`Lyo.Tts.AwsPolly`](../Lyo.Tts.AwsPolly/README.md)
- [`Lyo.Tts`](../Lyo.Tts/README.md) / [`Lyo.Tts.Models`](../Lyo.Tts.Models/README.md)
- [`Lyo.Web.Components`](../../../Integration/Web/Lyo.Web.Components/README.md)
