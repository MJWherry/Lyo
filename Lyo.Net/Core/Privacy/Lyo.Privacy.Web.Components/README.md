# Lyo.Privacy.Web.Components

Blazor (Server / Interactive) workbench components for [`Lyo.Privacy`](../Lyo.Privacy/README.md). Operators can preview, compare, and tune redaction policies without a host config round-trip.

## Components

| Component | What it does |
| ----------------------------- | ----------------------------------------------------------------------------------------------------------------------------------- |
| `PrivacyWorkbench` | Top-level workbench shell. Arranges the panels below into a side-by-side review layout. |
| `RedactionPolicyBuilderPanel` | Interactive `RedactionPolicyBuilder` editor: turn categories on or off, configure custom regex rules, tweak placeholders. |
| `RedactionPresetToolbar` | Preset buttons such as "PII only", "Strict", and "Permissive". Fires a completed policy. |
| `TextRedactionPanel` | Free-text input plus a preview of `ITextRedactor.Redact(...)` output and a `RedactionOutcome` summary (categories matched, counts). |
| `JsonRedactionPanel` | JSON input plus a preview of `IStructuredRedactor.Redact(...)` output with per-path redaction highlights. |
| `PolicyFingerprintChip` | Shows the active policy's name and fingerprint hash so reviewers can compare what production applied against the workbench. |

## Wiring into a host

These components take `ITextRedactor` and `IStructuredRedactor` from DI; register them via [`Lyo.Privacy.AspNetCore`](../Lyo.Privacy.AspNetCore/README.md) before mounting `<PrivacyWorkbench />` on a route. Layout and dialog plumbing use MudBlazor (via `Lyo.Web.Components`).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Privacy` (direct, lyo)
- `Lyo.Web.Primitives` (direct, lyo)
- `MudBlazor` `9.3` (direct, third-party)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Json` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Query.Evaluation` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Lyo.Validation.Models` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Collections.Immutable` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)