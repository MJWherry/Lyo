# Lyo.Privacy.Web.Components

Blazor (Server / Interactive) workbench components for [`Lyo.Privacy`](../Lyo.Privacy/README.md). Lets operators preview, compare, and tune redaction policies without
round-tripping through a host config edit.

## Components

| Component                         | Purpose                                                                                                                                  |
|-----------------------------------|------------------------------------------------------------------------------------------------------------------------------------------|
| **`PrivacyWorkbench`**            | Top-level workbench shell — composes the panels below into a side-by-side review layout.                                                 |
| **`RedactionPolicyBuilderPanel`** | Interactive `RedactionPolicyBuilder` editor: enable / disable categories, configure custom regex rules, tweak placeholders.              |
| **`RedactionPresetToolbar`**      | One-click presets (e.g. "PII only", "Strict", "Permissive") — emits a fully-built policy.                                                |
| **`TextRedactionPanel`**          | Free-text input + preview of `ITextRedactor.Redact(...)` output and `RedactionOutcome` summary (categories matched, counts).             |
| **`JsonRedactionPanel`**          | JSON input + preview of `IStructuredRedactor.Redact(...)` output with per-path redaction highlights.                                     |
| **`PolicyFingerprintChip`**       | Compact display of the active policy's name + fingerprint hash so reviewers can compare what was applied in production vs the workbench. |

## Host integration

These components consume `ITextRedactor` and `IStructuredRedactor` from DI; register them via [`Lyo.Privacy.AspNetCore`](../Lyo.Privacy.AspNetCore/README.md) before mounting
`<PrivacyWorkbench />` on a route. The workbench uses MudBlazor (via `Lyo.Web.Components`) for layout and dialog plumbing.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Privacy` — (direct, lyo)
- `Lyo.Web.Components` — (direct, lyo)
- `MudBlazor` `9.3` — (direct, third-party)
- `Lyo.Api.Client` — (transitive, lyo)
- `Lyo.Api.Models` — (transitive, lyo)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.DateAndTime` — (transitive, lyo)
- `Lyo.Diagnostic` — (transitive, lyo)
- `Lyo.Encryption` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Lyo.Hashing` — (transitive, lyo)
- `Lyo.IO.Temp` — (transitive, lyo)
- `Lyo.KeyStore` — (transitive, lyo)
- `Lyo.Metrics` — (transitive, lyo)
- `Lyo.PackageMetadata` — (transitive, lyo)
- `Lyo.Query.Models` — (transitive, lyo)
- `Lyo.Result` — (transitive, lyo)
- `Lyo.Streams` — (transitive, lyo)
- `Lyo.Validation` — (transitive, lyo)
- `Blazored.LocalStorage` `4.5.0` — (transitive, third-party)
- `BouncyCastle.Cryptography` `2.6.2` — (transitive, third-party, netstandard2.0)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` — (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `System.Buffers` `4.6.1` — (transitive, microsoft, netstandard2.0)
- `System.Collections.Immutable` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` — (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (transitive, microsoft, netstandard2.0)