# Lyo.Diff

Side-by-side comparison utilities for **human-readable text** and **arbitrary object graphs**. The package exposes a small **`IDiffService` façade** backed by two subsystems you can also inject independently.

## Components

### Text diffing (`Lyo.Diff.Text`)

- **`ITextTokenizer`** (**`TextTokenizer`**) — splits input into **`TextToken[]`** using **`TextDiffOptions`** (line vs word vs character modes in **`TextTokenizeMode`**, ignore case, ignore whitespace, **`MaxTokensPerSide` safety cap**—exceeding the cap throws to avoid runaway memory on pathological input).
- **`ITextDiffService`** (**`TextDiffService`**) — runs a **Myers** diff over parallel token streams, returns **`TextDiffResult`** with ordered **`TextDiffChunk`** segments tagged **`TextDiffKind`** (equal, insert, delete).
- **`MyersDiffCalculator`** — core algorithm implementation over pre-tokenized spans.

**Typical usage:** show user-facing “what changed” snippets, render red/green code review panes, produce patch-like summaries for moderation or audit logs—**not** a unified diff file format for `patch(1)`.

### Object graph diffing (`Lyo.Diff.ObjectGraph`)

- **`IObjectGraphDiffService`** (**`ObjectGraphDiffService`**) — walks two object instances of the same nominal type (or compatible graphs), compares reachable **properties and nested objects** according to **`ObjectGraphDiffOptions`**, and yields a list of **`ObjectGraphDifference`** entries (path/context + old/new values at leaves).
- **`ObjectGraphLeafContext`** captures where in the graph the change occurred for UI or logging.

**Typical usage:** “settings before vs after”, “sync drift” detection, Discord/bot integrations that need to describe **what changed** in a structured model (see [`Lyo.Discord.Bot`](../../../Integration/Discord/Lyo.Discord.Bot/README.md) for a real consumer).

### Facade

- **`IDiffService`** aggregates **`Text`** and **`Objects`** so a single injectable service suffices for mixed workflows.

## Registration

```csharp
using Lyo.Diff;

builder.Services.AddLyoDiff(); // ITextTokenizer, ITextDiffService, IObjectGraphDiffService, IDiffService
```

All defaults are **singletons** (stateless services except options you pass per call).

## Design notes

- **No dependency on EF or JSON**—inputs are **`string`** or CLR objects you already hydrated.
- **No automatic persistence**—callers snapshot “before” states if needed.
- **Security:** diffing arbitrary user text can leak secrets in logs—sanitize **`TextDiffResult`** before exposing externally (same caution as storing raw payloads).

## See also

- [`Lyo.Diagnostic`](../../Diagnostic/Lyo.Diagnostic/README.md) — stack/metadata enrichment when diff output feeds triage tooling.
