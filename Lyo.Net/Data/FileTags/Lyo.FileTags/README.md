# Lyo.FileTags

Generic file tags used by file storage and by media. `FileTagKey` is the catalog (`title`, `artist`, `album`, `album_artist`, `comment`, `genre`, `date`, `author`, `subject`,
`description`, `copyright`, `keywords`, plus `Custom`). The result type is `Lyo.FileTags.FileTags`. Call sites use that qualified name or an alias. `IFileTagService` uses the first
registered provider whose `CanRead` or `CanWrite` is true. No matching provider returns a failed result. This package does not reference ffmpeg or ImageSharp.

File storage copies a successful read into `FileStoreResult.Metadata` as a `fileTags` object before compress or encrypt. Convert dictionaries in [
`Lyo.Media.Models`](../../Media/Lyo.Media.Models/README.md) use the same keys. Picture pixel size stays on [`IImageService`](../../Images/Lyo.Images/README.md).

## Features

- **One catalog.** Built-in ids match the `fileTags` JSON names and ffmpeg `-metadata` keys. `Custom("composer")` round-trips through `TryFromId`.
- **Dispatcher.** `FileTagService` walks readers and writers in registration order. A `.pdf` matches neither the ffmpeg nor the image provider.
- **Metadata merge.** `FileTagMetadata.Merge` writes `{ "fileTags": { "title": "Show" } }` and keeps every other caller key. A new read replaces `fileTags`.

## Examples

### Register both providers

```csharp
using Lyo.FileTags;
using Lyo.FileTags.FFmpeg;
using Lyo.FileTags.Images;

services.AddFFmpegFileTags();
services.AddImageFileTags();
var tags = scope.ServiceProvider.GetRequiredService<IFileTagService>();
var read = await tags.ReadAsync("clip.mp3");
```

## Public types

| Type                                              | Description                                                    |
|---------------------------------------------------|----------------------------------------------------------------|
| FileTagKey                                        | Built-in tag ids plus Custom. TryFromId / TryFromName.         |
| FileTags / FileTagWriteRequest                    | Read result, and a write with ClearExisting.                   |
| IFileTagReader / IFileTagWriter / IFileTagService | Provider gates and the first-match dispatcher.                 |
| FileTagMetadata                                   | Merges tags into the file-storage metadata object as fileTags. |
| Extensions                                        | AddFileTagService().                                           |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `System.Text.Json` `10.0.5` (direct, microsoft, netstandard2.0)