# Lyo.Diagnostic.Web.Components

Blazor (Server / Interactive) workbench for analyzing and triaging .NET stack traces and exception payloads with [`Lyo.Diagnostic`](../Lyo.Diagnostic/README.md). The workbench is
deliberately pure-UI — paste, view, and classify; nothing is persisted by the components themselves.

## Components

| Component                          | Purpose                                                                                                                                                          |
|------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **`DiagnosticWorkbench`**          | Top-level shell. Composes the panels below into a side-by-side input/output review layout.                                                                       |
| **`StackTraceDecodePanel`**        | Paste raw stack trace text. Decodes via `IStackTraceDecoder` and renders structured frames.                                                                      |
| **`DecodedStackTraceSummary`**     | Compact summary chip with top frame, exception type, framework signature.                                                                                        |
| **`StackFrameTable`**              | Tabular view of decoded frames (`method`, `file`, `line`, `assembly`) with row-level highlighting for user-code frames.                                          |
| **`SanitisedStackTracePanel`**     | Renders the sanitised stack trace (paths/PII trimmed) suitable for inclusion in customer-facing error pages.                                                     |
| **`ExceptionTypeClassifierPanel`** | Runs the pasted exception type through `IExceptionTypeClassifier` and displays the resulting category (`Transient`, `Programming`, `Validation`, `Security`, …). |

## Host integration

The workbench resolves `IStackTraceDecoder` and `IExceptionTypeClassifier` from DI — register them via `Lyo.Diagnostic` extensions before mounting `<DiagnosticWorkbench />`.
MudBlazor is used for layout (via `Lyo.Web.Components`).

## Related projects

- [`Lyo.Diagnostic`](../Lyo.Diagnostic/README.md)
- [`Lyo.Diagnostic.AspNetCore`](../Lyo.Diagnostic.AspNetCore/README.md)
- [`Lyo.Web.Components`](../../../Integration/Web/Lyo.Web.Components/README.md)
