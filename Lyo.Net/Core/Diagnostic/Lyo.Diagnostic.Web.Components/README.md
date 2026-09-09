# Lyo.Diagnostic.Web.Components

Blazor (Server / Interactive) workbench that uses [`Lyo.Diagnostic`](../Lyo.Diagnostic/README.md) to analyze and triage .NET stack traces and exception payloads. The workbench is UI only — paste, view, and classify. Nothing is persisted by the components.

## Components

| Component | What it does |
| ------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| `DiagnosticWorkbench` | Top-level shell. Arranges the panels below into a side-by-side input/output review layout. |
| `StackTraceDecodePanel` | Drop in raw stack-trace text. `IStackTraceDecoder` decodes it and the panel shows structured frames. |
| `DecodedStackTraceSummary` | Compact summary chip with top frame, exception type, and framework signature. |
| `StackFrameTable` | Decoded frames in a table (`method`, `file`, `line`, `assembly`). Frames from user code get extra styling. |
| `SanitisedStackTracePanel` | The sanitised stack trace (paths/PII trimmed) for error pages customers see. |
| `ExceptionTypeClassifierPanel` | Runs the pasted exception type through `IExceptionTypeClassifier` and shows the category (`Transient`, `Programming`, `Validation`, `Security`, …). |

## Wiring into a host

DI supplies `IStackTraceDecoder` and `IExceptionTypeClassifier` to the workbench. Register them with `Lyo.Diagnostic` extensions before you mount `<DiagnosticWorkbench />`. MudBlazor layout comes from `Lyo.Web.Components`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Diagnostic` (direct, lyo)
- `Lyo.Web.Primitives` (direct, lyo)
- `MudBlazor` `9.3` (direct, third-party)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Json` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Query.Evaluation` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Lyo.Validation.Models` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft)