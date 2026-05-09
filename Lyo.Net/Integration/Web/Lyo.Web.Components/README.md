# Lyo.Web.Components

Blazor components library for the Lyo web UI framework with MudBlazor integration.

## File upload (`Lyo.Web.Components.FileUpload`)

**`LyoFileUpload`** wraps **`MudFileUpload`** with optional drag-and-drop, progress chips, and temp-file streaming. For long file names in tight layouts:

- **`ChipFileNameMaxLength`** — Short label inside each completed chip (`prefix…ext`); full name in a tooltip when truncated.
- **`ChipMaxWidthCss`** — Optional CSS `max-width` on the chip (e.g. `100%` or `min(100%,12rem)`) so chips stay inside narrow columns; combine with **`ChipFileNameMaxLength`**.

Completed chips always render the shortened display name (not the raw full name when under the max length), so truncation stays consistent in both chip layouts.

## Related projects

- [`Lyo.Api.Client`](../../Api/Lyo.Api.Client/README.md)
- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
- [`Lyo.Csv`](../../../Data/Csv/Lyo.Csv/README.md)
- [`Lyo.IO.Temp`](../../../Data/IOTemp/Lyo.IO.Temp/README.md)
- [`Lyo.Pdf`](../../../Data/Pdf/Lyo.Pdf/README.md)
- [`Lyo.Query.Models`](../../../Data/Query/Lyo.Query.Models/README.md)
- [`Lyo.Result`](../../../Core/Result/Lyo.Result/README.md)
- [`Lyo.Validation`](../../../Core/Validation/Lyo.Validation/README.md)
- [`Lyo.Xlsx`](../../../Data/Xlsx/Lyo.Xlsx/README.md)
- [`Lyo.Encryption`](../../../Security/Encryption/Lyo.Encryption/README.md)