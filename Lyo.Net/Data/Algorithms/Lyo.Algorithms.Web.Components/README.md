# Lyo.Algorithms.Web.Components

MudBlazor editors for every algorithm kind. The catalog scrolls in its own pane and can delete one key or the checked keys. The Editor tab holds the draft. The Runner tab edits declared parameter values and posts preview only when Run is clicked. The component does not open a database and does not evaluate expressions in the browser. Register samples with AddAlgorithmWorkbench. The draft and values persist in ClientStore under algorithm_workbench_state_v1 at 450 ms.

## Features

- **AlgorithmWorkbench.** Scrollable catalog, delete, and delete selected. Editor and Runner tabs. Presets load the fishing derby, attack, and schedule drafts with their sample values.
- **Type panel.** Dense name and alias rows, plus call rows. Range and linear also edit inputType. An extra field accepts a host enum name. Encrypt, required, and options are not stored.
- **Kind editors.** Expression, named, discrete map, range band, linear, and composite. Composite children are an ordered stack of a catalog ref or an inline scorer.
- **Expression surface.** Chips are the identifiers already in the source. The caret dropdown filters bag names, the compiler allowlist, and Algo / score catalog keys.
- **Runner.** One LyoParameterValueGrid entry per declared parameter. Run posts preview. A failed run clears the previous value and shows the response message.
- **Breakdown.** Shows the JSON value and the step tree. A non-numeric step shows Value and leaves Points at 0.

## Examples

### Register the workbench

```csharp
services.AddAlgorithmClient(sp => sp.GetRequiredService<IApiClient>());
services.AddAlgorithmWorkbench(context => {
    context.Add("Atk", "double", "3");
    context.Add("Def", "double", "1");
});
```

## Browser script

wwwroot/scripts/algorithmCaret.js is imported from `_content/Lyo.Algorithms.Web.Components/scripts/algorithmCaret.js` so the expression palette can read and restore the caret.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Algorithms` (direct, lyo)
- `Lyo.Algorithms.Client` (direct, lyo)
- `Lyo.Algorithms.Models` (direct, lyo)
- `Lyo.Http.Client` (direct, lyo)
- `Lyo.Parameters` (direct, lyo)
- `Lyo.Web.Components` (direct, lyo)
- `MudBlazor` `9.3` (direct, third-party)
- `Lyo.Api.Client` (transitive, lyo)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.EntityReference.Models` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.IO.FileSystem` (transitive, lyo)
- `Lyo.IO.Temp` (transitive, lyo)
- `Lyo.Json` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Query.Evaluation` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.RateLimit` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Schedule.Models` (transitive, lyo)
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
- `Microsoft.Bcl.TimeProvider` `8.0.1` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)