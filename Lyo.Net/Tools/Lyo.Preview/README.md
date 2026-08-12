# Lyo.Preview

Cross-platform preview in the system default browser. The default implementation, `BrowserPreview`, spins up an `HttpListener` on `127.0.0.1` (random free port), registers one byte
buffer per call under a `/p/{id}` path, opens the URL in the OS browser, and releases the entry as soon as the browser fetches it. CSV and XLSX content is converted to an HTML
table before serving so the browser doesn't try to download it.

**Supported types:** PDF, HTML, PNG, JPEG, GIF, BMP, SVG, WebP, TIFF, TXT, JSON, XML, CSV, XLSX. Anything else throws `NotSupportedException`. XLSX additionally requires an
`IXlsxService` to be available in DI (see [DI registration](#di-registration)).

## Examples

### DI registration

```csharp
// Optional: needed for table rendering of XLSX (and to upgrade CSV from the built-in
// minimal converter to the full Lyo.Csv path).
services.AddCsvService();
services.AddXlsxService();

services.AddPreviewService();
```

### Usage

```csharp
// File or URL — type from extension
await preview.PreviewFileAsync("/path/to/image.png");
await preview.PreviewFileAsync("https://example.com/doc.pdf");

// Stream/bytes — caller must pass FileTypeInfo
await preview.PreviewAsync(stream, FileTypeInfo.Pdf);
await preview.PreviewAsync(bytes, FileTypeInfo.Png);
await preview.PreviewAsync(Encoding.UTF8.GetBytes("<h1>Hi</h1>"), FileTypeInfo.Html);

// Static (no DI, no XLSX)
await Preview.FileAsync("/path/to/file.pdf");
await Preview.Async(bytes, FileTypeInfo.Png);
Preview.ResetDefault();
```

### Static convenience (`Preview`)

```csharp
static IPreviewService Default { get; } // BrowserPreview built on first access (no DI, no XLSX)
static Task<string?> FileAsync(string pathOrUrl, CancellationToken ct = default);
static Task<string?> Async (Stream stream, FileTypeInfo fileType, CancellationToken ct = default);
static Task<string?> Async (byte[] bytes, FileTypeInfo fileType, CancellationToken ct = default);
static void ResetDefault(); // drops the cached instance (handy in tests)
```

## API — `IPreviewService`

```csharp
Task<string?> PreviewFileAsync(string pathOrUrl, CancellationToken ct = default);
Task<string?> PreviewAsync(Stream stream, FileTypeInfo fileType, CancellationToken ct = default);
Task<string?> PreviewAsync(byte[] bytes, FileTypeInfo fileType, CancellationToken ct = default);
```

- `PreviewFileAsync` reads the file and infers `FileTypeInfo` from the extension via `FileTypeInfo.FromFilePath`. Throws if the extension maps to an unsupported type.
- The stream/bytes overloads require an explicit `FileTypeInfo` since there's nothing to sniff.
- All three return the served URL on success, or `null` if the input was empty.

## DI registration

- **CSV** — if `ICsvService` is registered, `ExportToHtmlTable(bytes)` is used. If not, `BrowserPreview` falls back to a built-in minimal CSV → `<table>` converter
  ( `ConvertCsvToHtml`) so CSV preview still works.
- **XLSX** — `ExportToHtmlTable(bytes)` via `IXlsxService` is required. Without `IXlsxService`, `BrowserPreview` throws (`NotSupportedException` at validation time when used
  through DI, or via `OperationHelpers.ThrowIf` if a scope factory is present but the service was not registered).

## How the server works

- Encodes CSV/XLSX content to HTML if needed and tags it with the right `Content-Type` (`text/html; charset=utf-8` for tables, otherwise the `FileTypeInfo` MIME type).
- Increments `_requestId`, stores a `PendingContent(bytes, contentType)` under path `p/{id}`, and returns `http://127.0.0.1:{port}/p/{id}`.
- Calls `OpenBrowser(url)` — `ProcessStartInfo { UseShellExecute = true }` on Windows, `open` on macOS, `xdg-open` on Linux, with a `UseShellExecute = true` fallback if the
  platform-specific command fails.
- The browser hits the URL, the request handler atomically `TryRemove`s the entry and writes the bytes — so each generated URL is single-use.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` — (direct, lyo)
- `Lyo.Csv.Models` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Xlsx.Models` — (direct, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (direct, microsoft)
- `Lyo.DataTable.Models` — (transitive, lyo)
- `Lyo.Result` — (transitive, lyo)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)