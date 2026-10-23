# Lyo.FileTags.Images

Picture tags for [`Lyo.FileTags`](../Lyo.FileTags/README.md). Extensions are jpeg, png, webp, gif, and tiff. Read maps EXIF artist, date, description, and copyright onto
`FileTagKey`. Any other EXIF name is `Custom`. Write sets those mapped tags and saves the image. Width, height, and format stay on [
`IImageService.GetMetadataAsync`](../../Images/Lyo.Images/README.md).

## Features

- **Read.** `Artist` becomes `FileTagKey.Artist`. `DateTimeOriginal` wins over `DateTime` and `DateTimeDigitized` for `FileTagKey.Date`.
- **Write.** Sets artist, image description, copyright, and original date. `ClearExisting` replaces the EXIF profile first.
- **Not pixels.** This provider does not resize, transcode, or report width and height.

## Examples

### Read the artist on a JPEG

```csharp
var images = new ImageFileTagProvider();
var read = await images.ReadAsync("photo.jpg");
if (read.IsSuccess && read.Data!.Tags.TryGetValue(FileTagKey.Artist, out var artist))
    Console.WriteLine(artist);
```

## Public types

| Type                 | Description                                               |
|----------------------|-----------------------------------------------------------|
| ImageFileTagProvider | IFileTagReader and IFileTagWriter for picture extensions. |
| Extensions           | AddImageFileTags().                                       |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.FileTags` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `SixLabors.ImageSharp` `3.1.12` (direct, third-party)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)