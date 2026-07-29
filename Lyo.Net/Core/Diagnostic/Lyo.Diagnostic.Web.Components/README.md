# Lyo.Diagnostic.Web.Components

Blazor (Server / Interactive) workbench for analyzing and triaging .NET stack traces and exception payloads with [`Lyo.Diagnostic`](../Lyo.Diagnostic/README.md). The workbench is
deliberately pure-UI — paste, view, and classify; nothing is persisted by the components themselves.

## Components

| Component | Purpose |
| ---------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **`DiagnosticWorkbench`** | Top-level shell. Composes the panels below into a side-by-side input/output review layout. |
| **`StackTraceDecodePanel`** | Paste raw stack trace text. Decodes via `IStackTraceDecoder` and renders structured frames. |
| **`DecodedStackTraceSummary`** | Compact summary chip with top frame, exception type, framework signature. |
| **`StackFrameTable`** | Tabular view of decoded frames (`method`, `file`, `line`, `assembly`) with row-level highlighting for user-code frames. |
| **`SanitisedStackTracePanel`** | Renders the sanitised stack trace (paths/PII trimmed) suitable for inclusion in customer-facing error pages. |
| **`ExceptionTypeClassifierPanel`** | Runs the pasted exception type through `IExceptionTypeClassifier` and displays the resulting category (`Transient`, `Programming`, `Validation`, `Security`, …). |

## Host integration

The workbench resolves `IStackTraceDecoder` and `IExceptionTypeClassifier` from DI — register them via `Lyo.Diagnostic` extensions before mounting `<DiagnosticWorkbench />`. MudBlazor is used for layout (via `Lyo.Web.Components`).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Diagnostic` — (direct, lyo)
- `Lyo.Web.Components` — (direct, lyo)
- `MudBlazor` `9.3` — (direct, third-party)
- `Lyo.Api.Client` — (transitive, lyo)
- `Lyo.Api.Models` — (transitive, lyo)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.DateAndTime` — (transitive, lyo)
- `Lyo.Encryption` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Lyo.Hashing` — (transitive, lyo)
- `Lyo.IO.Temp` — (transitive, lyo)
- `Lyo.Keystore` — (transitive, lyo)
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
- `System.Buffers` `4.6.0` — (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` — (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (transitive, microsoft, netstandard2.0)