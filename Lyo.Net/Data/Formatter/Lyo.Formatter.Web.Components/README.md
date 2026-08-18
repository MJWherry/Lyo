# Lyo.Formatter.Web.Components

WASM-safe Blazor / MudBlazor components for [`Lyo.Formatter`](../Lyo.Formatter/README.md). Two components share a **`LyoFormatterLiveSession`** so a host can place the template text box in one region and the live preview in another. Formatting runs in-process via **`IFormatterService`** (no HTTP). Compatible with InteractiveWebAssembly and InteractiveServer; this package does **not** reference `Microsoft.AspNetCore.App`.

## Examples

### Split editor and preview

```razor
@inject IFormatterService Formatter

<MudGrid>
    <MudItem xs="12" md="6">
        <LyoFormatterTemplateEditor Session="_session" DebounceInterval="300" />
    </MudItem>
    <MudItem xs="12" md="6">
        <LyoFormatterPreview Session="_session" />
    </MudItem>
</MudGrid>

@code {
    private LyoFormatterLiveSession _session = default!;

    protected override void OnInitialized()
    {
        _session = new(Formatter) {
            Template = "Hello, {Name}! Count: {Count:N0}",
            Context = new Dictionary<string, object?> { ["Name"] = "Ada", ["Count"] = 42 }
        };
    }
}
```

## Components

| Component | Purpose |
| -------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **`LyoFormatterTemplateEditor`** | Template textarea with an in-place colored overlay of `{placeholders}`. Typing `{` opens a caret-anchored dropdown of session context keys; object/dictionary values also list nested properties (`User.Address.City`). Typing updates the overlay immediately; preview rebuilds after `DebounceInterval` (default 300 ms). Chip row under the field is hoverable so keys link to preview values. `Session` parameter wins over a cascaded session. |
| **`LyoFormatterPreview`** | Read-only annotated output. Each replacement is a span with the same per-key color as the editor token. Hover a replacement to emphasize the matching `{key}` (and vice versa via editor chips). |

## Session and highlighting

- **`LyoFormatterLiveSession`** — parent-owned `INotifyPropertyChanged` state: `Template`, `Context`, `HoveredPlaceholder`, `DebounceInterval`, `TemplateSegments`, `PreviewSegments`. Call `RefreshPreview()` after setting the initial template/context so the first paint is not empty. Dispose the session with the page.
- **Stable colors** — `LyoFormatterPlaceholderPalette` hashes the placeholder key (ordinal ignore-case) to an HSL hue so `{Name}` and its replacement always match. Fills are `color-mix`ed into `--mud-palette-surface` so light and dark themes keep contrast. Not re-randomized per render.
- **Hover** — sets `Session.HoveredPlaceholder`; matching spans get a stronger fill and outline.
- **Unresolved** — missing context (`MaintainTokens`) uses a muted dashed underline instead of a key color.
- **Autocomplete** — `LyoFormatterContextCatalog` walks `Session.Context` (dictionary keys or public properties, nested up to 3 levels). Typing `{` opens a floating list under the caret of keys and nested object properties (`User`, `User.Address.City`). Further typing filters the list (prefix match on the path or a dotted segment), including inside an already-closed `{key}`. Arrow/Enter/Tab/Escape move and insert; the active row scrolls into view.
- Optional **`CascadingValue`** of the session so distant editor/preview pairs need not pass `Session` on every call; an explicit `Session` parameter still wins.

## Host integration

Call `services.AddFormatterService()` from [`Lyo.Formatter`](../Lyo.Formatter/README.md). Create one `LyoFormatterLiveSession` per editor/preview pair (inject `IFormatterService`). Supply `Context` as a DTO or `IReadOnlyDictionary<string, object?>`. There is no context editor in this package.

## WASM

No `IHttpContextAccessor`, no server-only framework reference, no formatting via HTTP. Overlay and textarea share a CSS grid cell so they scroll together. Caret position and the `{` autocomplete dropdown use a small JS module from this package's `wwwroot` (imported by the editor; no host `<script>` tag). Works under InteractiveWebAssembly when the host registers `IFormatterService` and references this package.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Formatter` — (direct, lyo)
- `Lyo.Web.Components` — (direct, lyo)
- `MudBlazor` `9.3` — (direct, third-party)
- `Lyo.Api.Client` — (transitive, lyo)
- `Lyo.Api.Models` — (transitive, lyo)
- `Lyo.Cache` — (transitive, lyo)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.Compression` — (transitive, lyo)
- `Lyo.DataTable.Models` — (transitive, lyo)
- `Lyo.DateAndTime` — (transitive, lyo)
- `Lyo.Diagnostic` — (transitive, lyo)
- `Lyo.Encryption` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Lyo.Hashing` — (transitive, lyo)
- `Lyo.Health` — (transitive, lyo)
- `Lyo.IO.Temp` — (transitive, lyo)
- `Lyo.KeyStore` — (transitive, lyo)
- `Lyo.Metrics` — (transitive, lyo)
- `Lyo.PackageMetadata` — (transitive, lyo)
- `Lyo.Query` — (transitive, lyo)
- `Lyo.Query.Models` — (transitive, lyo)
- `Lyo.Result` — (transitive, lyo)
- `Lyo.Streams` — (transitive, lyo)
- `Lyo.Validation` — (transitive, lyo)
- `Blazored.LocalStorage` `4.5.0` — (transitive, third-party)
- `BouncyCastle.Cryptography` `2.6.2` — (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` — (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` — (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `SmartFormat.NET` `3.6.1` — (transitive, third-party)
- `System.Buffers` `4.6.1` — (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` — (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (transitive, microsoft, netstandard2.0)