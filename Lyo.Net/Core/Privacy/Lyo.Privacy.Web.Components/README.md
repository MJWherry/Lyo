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

## Related projects

- [`Lyo.Privacy`](../Lyo.Privacy/README.md)
- [`Lyo.Privacy.AspNetCore`](../Lyo.Privacy.AspNetCore/README.md)
- [`Lyo.Web.Components`](../../../Integration/Web/Lyo.Web.Components/README.md)
