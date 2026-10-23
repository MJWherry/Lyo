# Lyo.FileTags.FFmpeg

Audio and video tags for [`Lyo.FileTags`](../Lyo.FileTags/README.md). Extensions are mp3, m4a, flac, wav, ogg, mp4, mov, mkv, and webm. Images are not claimed. Read merges ffprobe
format tags over stream tags. Write is `-c copy`, optional `-map_metadata -1`, then `-metadata`. It does not re-encode. Convert-time `-metadata` during an encode stays in [
`Lyo.FFmpeg`](../../Media/Lyo.FFmpeg/README.md).

## Features

- **Read.** Format `title` beats a stream `title`. An unknown name such as `composer` stays `FileTagKey.Custom`.
- **Write.** Stream copy onto a temp file, then replace. `ClearExisting` emits `-map_metadata -1` before `-c copy` and `-metadata`.
- **Paths.** `FFmpegOptions.FFmpegPath` and `FfprobePath`. Null uses the tools on PATH.

## Examples

### Set a title without re-encoding

```csharp
var tags = new FFmpegFileTagProvider(new FFmpegOptions());
await tags.WriteAsync("tone.wav", new FileTagWriteRequest(new Dictionary<FileTagKey, string> {
    [FileTagKey.Title] = "Show"
}));
```

## Public types

| Type                  | Description                                                       |
|-----------------------|-------------------------------------------------------------------|
| FFmpegFileTagProvider | IFileTagReader and IFileTagWriter for audio and video extensions. |
| Extensions            | AddFFmpegFileTags() and AddFFmpegFileTags(FFmpegOptions).         |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.FFmpeg` (direct, lyo)
- `Lyo.FileTags` (direct, lyo)
- `CliWrap` `3.10.2` (direct, third-party)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.FFmpeg.Models` (transitive, lyo)
- `Lyo.Media.Models` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)