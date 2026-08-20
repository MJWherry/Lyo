# Lyo.Diff

Side-by-side comparison for human-readable text and object graphs. `IDiffService` is backed by two subsystems you can also inject independently.

## Examples

### Register services

```csharp
using Lyo.Diff;

builder.Services.AddLyoDiff(); // ITextTokenizer, ITextDiffService, IObjectGraphDiffService, IDiffService
```

## Text diffing (`Lyo.Diff.Text`)

- **`ITextTokenizer` (`TextTokenizer`).** Splits input into `TextToken[]` using `TextDiffOptions` (line vs word vs character modes in `TextTokenizeMode`, ignore case, ignore whitespace, `MaxTokensPerSide` safety cap). Exceeding the cap throws to avoid runaway memory on pathological input.
- **`ITextDiffService` (`TextDiffService`).** Runs a Myers diff over parallel token streams. Returns `TextDiffResult` with ordered `TextDiffChunk` segments tagged `TextDiffKind` (equal, insert, delete).
- **`MyersDiffCalculator`.** Algorithm implementation over pre-tokenized spans.

## Object graph diffing (`Lyo.Diff.ObjectGraph`)

- **`IObjectGraphDiffService` (`ObjectGraphDiffService`).** Walks two object instances of the same nominal type (or compatible graphs), compares reachable properties and nested objects according to `ObjectGraphDiffOptions`, and yields a list of `ObjectGraphDifference` entries (path/context + old/new values at leaves).
- **`ObjectGraphLeafContext`.** Captures where in the graph the change occurred for UI or logging.

## Facade

- **`IDiffService`.** Exposes `Text` and `Objects` so one injection covers both.

## Registration

All defaults are singletons. Services are stateless except options you pass per call.

## Design notes

- No dependency on EF or JSON. Inputs are `string` or CLR objects you already hydrated.
- No automatic persistence. Callers snapshot "before" states if needed.
- **Security.** Diffing arbitrary user text can leak secrets in logs. Sanitize `TextDiffResult` before exposing it, same caution as storing raw payloads.

## See also

- [`Lyo.Diagnostic`](../../Diagnostic/Lyo.Diagnostic/README.md). Stack/metadata enrichment when diff output feeds triage tooling.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `System.Buffers` `4.6.1` (direct, microsoft, netstandard2.0)