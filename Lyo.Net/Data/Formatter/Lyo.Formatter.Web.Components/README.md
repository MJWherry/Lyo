# Lyo.Formatter.Web.Components

Blazor / MudBlazor UI for [`Lyo.Formatter`](../Lyo.Formatter/README.md). `LyoFormatterTemplateEditor` and `LyoFormatterPreview` share a `LyoFormatterLiveSession`, so the host can put the template box in one region and the preview in another. Formatting runs in-process via `IFormatterService`. No HTTP. Works with InteractiveWebAssembly and InteractiveServer. This package does not reference `Microsoft.AspNetCore.App`.

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
| ---------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `LyoFormatterTemplateEditor` | Template textarea with an in-place colored overlay of `{placeholders}`. Typing `{` opens a dropdown of session context keys, anchored at the caret. Object and dictionary values also list nested properties (`User.Address.City`). Typing updates the overlay immediately. The preview rebuilds after `DebounceInterval` (default 300 ms). The chip row under the field is hoverable so keys link to preview values. A `Session` parameter wins over a cascaded session. |
| `LyoFormatterPreview` | Read-only annotated output. Each replacement is a span using the same per-key color as the editor token. Hover a replacement to emphasize the matching `{key}`. Editor chips do the reverse. |
| `LyoFormatterValueEditor` | The template editor used as the value editor for a formatter-typed parameter. Handles the JSON round-trip a parameter value needs (an empty template stores `null`), merges the view's context over the host default, and adds a collapsed preview plus an **Available tokens** list that inserts a token on click. Not rendered directly: `LyoTypeValueInput` picks it up from DI. |

## Formatter-typed parameters

Job and report parameters can be formatter-typed; the value is a template. `AddLyoFormatterValueEditor` makes this package's editor the one `Lyo.Web.Components` renders for those values, instead of a plain text field:

```csharp
builder.Services.AddFormatterService();
builder.Services.AddLyoFormatterValueEditor(c => c
    .Add("client", new { contact = new { emailAddress = "to@example.com" } }));
```

The registration takes the keys **only the host knows**, typically the objects a job worker binds with `AddContext` at run time, which nothing earlier in the stack can discover. Views layer their live keys on top through the `FormatterContext` parameter (`JobFormatterContext`, `ReportFormatterContext`), and `LyoFormatterExampleContext.Merge` combines them with the view's keys winning.

The token list matters more than the autocomplete. Autocomplete only helps once you already know to type `{` and roughly what to look for.

## Session and colors

- **`LyoFormatterLiveSession`.** Parent-owned `INotifyPropertyChanged` state: `Template`, `Context`, `HoveredPlaceholder`, `DebounceInterval`, `TemplateSegments`, `PreviewSegments`. After setting the initial template and context, call `RefreshPreview()` so the first paint is not empty. Dispose the session with the page.
- **Stable colors.** `LyoFormatterPlaceholderPalette` hashes the placeholder key (ordinal ignore-case) to an HSL hue so `{Name}` and its replacement always match. Fills are `color-mix`ed into `--mud-palette-surface` so light and dark themes keep contrast. Colors are not re-randomized on each render.
- **Hover.** Sets `Session.HoveredPlaceholder`. Matching spans receive a stronger fill and outline.
- **Unresolved.** Missing context (`MaintainTokens`) draws a muted dashed underline instead of a key color.
- **Autocomplete.** `LyoFormatterContextCatalog` walks `Session.Context` (dictionary keys or public properties, nested up to 3 levels) and always offers `DateTime.Now`, `DateTime.UtcNow`, and `DateTime.Now.AddDays(-1)`. Typing `{` opens a floating list under the caret of keys and nested object properties (`User`, `User.Address.City`). Further typing filters the list (prefix match on the path or a dotted segment), including inside an already-closed `{key}`. Arrow, Enter, Tab, and Escape move and insert. The active row scrolls into view.
- Optional `CascadingValue` of the session so distant editor/preview pairs need not pass `Session` on every call. An explicit `Session` parameter still takes precedence.

## Host integration

Call `services.AddFormatterService()` from [`Lyo.Formatter`](../Lyo.Formatter/README.md). Create one `LyoFormatterLiveSession` per editor/preview pair (inject `IFormatterService`). Supply `Context` as a DTO or `IReadOnlyDictionary<string, object?>`. This package has no context editor. Add `AddLyoFormatterValueEditor(...)` as well to cover formatter-typed parameter values.

## WASM

No `IHttpContextAccessor`, no server-only framework reference, and no formatting via HTTP. Overlay and textarea share a CSS grid cell so they scroll together. Caret position and the `{` autocomplete dropdown use a small JS module from this package's `wwwroot` (imported by the editor; no host `<script>` tag). Works under InteractiveWebAssembly when the host registers `IFormatterService` and references this package. Expression tokens (`DateTime.Now`, ternary, LINQ) preview in default interpreted WASM; WASM AOT / Native AOT cannot run the expression engine (SmartFormat-only placeholders still work).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Formatter` (direct, lyo)
- `Lyo.Web.Components` (direct, lyo)
- `MudBlazor` `9.3` (direct, third-party)
- `Lyo.Api.Client` (transitive, lyo)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Json` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Configuration` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Http.Client` (transitive, lyo)
- `Lyo.IO.Temp` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Query.Evaluation` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Lyo.Validation.Models` (transitive, lyo)
- `Lyo.Web.Primitives` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `Blazored.LocalStorage` `4.5.0` (transitive, third-party)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `DynamicExpresso.Core` `2.19.3` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `SmartFormat.NET` `3.6.1` (transitive, third-party)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)