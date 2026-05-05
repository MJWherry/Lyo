# Lyo.Preview

Cross-platform preview in the system default browser. Implementation: `BrowserPreview`.

Throws `NotSupportedException` for unsupported file types (e.g. XLSX, DOCX, ZIP).

**Supported types:** PDF, HTML, PNG, JPEG, GIF, BMP, SVG, WebP, TIFF, TXT, JSON, XML, CSV.

## API

- **PreviewFile(pathOrUrl)** – Local file or URL. Type from path/extension.
- **Preview(stream, FileTypeInfo)** – Stream content. FileTypeInfo required.
- **Preview(bytes, FileTypeInfo)** – Byte content. FileTypeInfo required.

## Usage

```csharp
// File or URL – type from extension
await preview.PreviewFileAsync("/path/to/image.png");
await preview.PreviewFileAsync("https://example.com/doc.pdf");

// Stream/bytes – caller must pass FileTypeInfo
await preview.PreviewAsync(stream, FileTypeInfo.Pdf);
await preview.PreviewAsync(bytes, FileTypeInfo.Png);
await preview.PreviewAsync(Encoding.UTF8.GetBytes("<h1>Hi</h1>"), FileTypeInfo.Html);
```

## Static

```csharp
await Preview.FileAsync("/path/to/file.pdf");
await Preview.Async(bytes, FileTypeInfo.Png);
```


## Dependencies

*(Synchronized from `Lyo.Preview.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                     | Version |
|---------------------------------------------|---------|
| `Microsoft.Extensions.Logging.Abstractions` | `[10,)` |

### Project references

- [`Lyo.Common`](../../Core/Common/Lyo.Common/README.md)
- [`Lyo.Csv.Models`](../../Data/Csv/Lyo.Csv.Models/README.md)
- [`Lyo.Exceptions`](../../Core/Lyo.Exceptions/README.md)
- [`Lyo.Xlsx.Models`](../../Data/Xlsx/Lyo.Xlsx.Models/README.md)