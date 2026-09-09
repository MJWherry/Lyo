# Lyo.Diff

Compare human-readable text and object graphs side by side. Two subsystems sit behind `IDiffService`, and you can inject either one on its own.

## Examples

### Register in DI

```csharp
using Lyo.Diff;

builder.Services.AddLyoDiff(); // ITextTokenizer, ITextDiffService, IObjectGraphDiffService, IDiffService
```

## Diffing text (`Lyo.Diff.Text`)

- **`ITextTokenizer` (`TextTokenizer`).** Uses `TextDiffOptions` to split input into `TextToken[]` (`TextTokenizeMode` chooses line, word, or character; ignore case; ignore whitespace; `MaxTokensPerSide` safety cap). The cap throws when exceeded so pathological input cannot run away with memory.
- **`ITextDiffService` (`TextDiffService`).** Myers-diffs two parallel token streams and returns a `TextDiffResult` whose ordered `TextDiffChunk` segments carry a `TextDiffKind` (equal, insert, delete).
- **`MyersDiffCalculator`.** The algorithm, applied to spans that are already tokenized.

## Diffing object graphs (`Lyo.Diff.ObjectGraph`)

- **`IObjectGraphDiffService` (`ObjectGraphDiffService`).** Walks two instances of the same nominal type (or compatible graphs), compares reachable properties and nested objects under `ObjectGraphDiffOptions`, and yields `ObjectGraphDifference` entries (path/context plus old/new leaf values).
- **`ObjectGraphLeafContext`.** Records where in the graph the change happened, for UI or logging.

## Facade

- **`IDiffService`.** Surfaces `Text` and `Objects` so a single injection covers both.

## Registration

Defaults register as singletons. Aside from options you pass per call, the services hold no state.

## Notes on design

- EF and JSON are not dependencies. You pass a `string` or CLR objects you already hydrated.
- Nothing is persisted automatically. Snapshot a "before" state yourself if you need one.
- **Security.** Diffing arbitrary user text can leak secrets in logs. Sanitize `TextDiffResult` before you expose it — the same caution as storing raw payloads.

## Related reading

- [`Lyo.Diagnostic`](../../Diagnostic/Lyo.Diagnostic/README.md). Enrich stack and metadata when triage tooling consumes the diff output.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `System.Buffers` `4.6.1` (direct, microsoft, netstandard2.0)