# Lyo.FFmpeg.Models

FFmpeg-specific options and command line model. Audio/video contracts live in [`Lyo.Media.Models`](../Lyo.Media.Models/README.md). No DI lives here. Register in [`Lyo.FFmpeg`](../Lyo.FFmpeg/README.md).

## Features

- **FFmpegOptions.** Binary paths, ForAudio-style defaults (codec/sample rate/channels/format as config strings), Hardware (worker GPU name, null means CPU), HardwareDevice, GlobalArguments, metrics, ProcessOutputMode, ProcessTimeout. SectionName = FFmpegOptions. Validate() at DI rejects an unknown Hardware name; do not mutate from a convert.
- **FFmpegCommand.** ExecutablePath plus ArgumentList. Arguments is display-only.

## Public types

| Type | Description |
| ----------------------- | -------------------------------------------------------------------------------------------------------------------- |
| FFmpegOptions | Binary paths, audio defaults, Hardware, HardwareDevice, GlobalArguments, metrics, ProcessOutputMode, ProcessTimeout. |
| FFmpegCommand | ExecutablePath plus ArgumentList. Arguments is display-only. |
| FFmpegProcessOutputMode | Suppress or Passthrough leftover stdout/stderr. |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Media.Models` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)