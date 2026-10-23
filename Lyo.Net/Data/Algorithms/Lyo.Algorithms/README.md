# Lyo.Algorithms

Compiles named C# algorithms and DynamicExpresso expressions once, then runs them through IAlgorithmRegistry. After the first compile, Execute is a delegate call with no database, no Task, no JSON, and no conversion. AddMemoryAlgorithmStore keeps the catalog in process. Postgres and SQLite are optional packages that implement the same store. Catalog rows are the Req and Res types in Lyo.Algorithms.Models. Runtime rows do not replace a startup key unless AllowRuntimeOverride is set.

## Features

- **Startup store.** Named C# via AddNamedAlgorithm and bag expressions via AlgorithmOptions.Expressions or AddExpressionAlgorithm. Compiled at registration, then frozen.
- **Runtime catalog.** IAlgorithmDefinitionStore takes Req and returns Res. It appends revisions, copies a snapshot on revert, and retargets mutable labels. Immutable labels stay put. Structured kinds store a JSON payload.
- **AlgorithmRef.** Latest, Revision, or Label. Latest and mutable labels follow new revisions. Pins and immutable labels do not.
- **Types.** A revision payload declares `result`, and optionally `inputType`, `parameters`, and `calls`. A missing payload, or a missing field, means double. `*_key` identifiers stay string. A declared call type must equal that child revision's result. Legal aliases are bool, byte, sbyte, short, ushort, int, uint, long, ulong, float, double, decimal, string, datetime, datetimeoffset, timespan, and one-level arrays of those. A host registers an enum before a revision may name it.
- **Expressions.** Math and Convert stay available. DateTime and DateTimeOffset allow Year, Month, Day, Hour, Minute, Second, Date, AddDays, AddMonths, AddYears, and AddHours. UtcDateTime is DateTimeOffset only. TimeSpan allows TotalDays, TotalHours, and Days. Length and Count are allowed on arrays and non-string collections. string members, LINQ, and loops are denied. Algo("key") and score("key") invoke the child as its result type.
- **Execute.** The first call compiles a delegate. Later calls run that delegate. Bag values must already be the declared type. JSON numbers, band edges, and weights are converted while compiling.
- **Context bag.** `AlgorithmContextBag` stores identifiers on a case-insensitive `ContextBag`. `Set` rejects dotted names. Compiled formulas read with `Read<T>`, which succeeds only when the stored value is already `T`. A factory runs once when `ReadDouble` or `TryGetAs<T>` reads the key. A factory that receives the bag can read sibling identifiers.
- **Scorers.** DiscreteMap, RangeBand, Linear, and Composite (Sum, WeightedSum, or Formula) compile to the revision result. A map can return a date, a band can select a string, and a line or weighted sum can run in decimal. Sum and WeightedSum stay numeric. Weights stay decimal or double coefficients. ExecuteWithBreakdownAsync returns the value plus nested steps. A numeric step keeps Points. Any other step leaves Points at 0 and sets Value. Execute does not allocate a step.

## Examples

### Register and execute

```csharp
services.AddAlgorithms();
services.AddMemoryAlgorithmStore();
services.AddNamedAlgorithm<PhysicalAttack, CombatStats, decimal>("attack.physical");

var value = await registry.ExecuteAsync<CombatStats, decimal>("attack.physical", stats, ct);
```

### Catalog revision and label

```csharp
await catalog.SaveRevisionAsync("attack.physical", new AlgorithmRevisionReq { Kind = AlgorithmKind.Expression, Source = "Atk * 2 - Def" }, ct);
await catalog.SetLabelAsync("attack.physical", "game.1.0", new AlgorithmLabelReq { Revision = 1, IsMutable = false }, ct);
await catalog.SetLabelAsync("attack.physical", "live", new AlgorithmLabelReq { Revision = 1, IsMutable = true }, ct);

var pinned = await registry.ExecuteAsync<AlgorithmContextBag, double>(AlgorithmRef.Label("attack.physical", "game.1.0"), bag, ct);
```

## Lookup

- ExecuteAsync(key) and AlgorithmRef.Latest check the startup store first, then the catalog.
- AlgorithmRef.Revision and AlgorithmRef.Label read the catalog only. A startup key still wins when AllowRuntimeOverride is false: the runtime store returns no algorithm.
- Kind Named stores a startup key. Execute fails if that key was not registered.
- A failed expression save does not append a revision. The previous latest lambda stays.

## Stores

AddMemoryAlgorithmStore is in this package and needs no database. Postgres and SQLite are Lyo.Algorithms.Postgres and Lyo.Algorithms.Sqlite. Call one store after AddAlgorithms. Hosts that only need startup C# can skip a store; resolving IAlgorithmCatalog without a store throws. HTTP catalog routes live in Lyo.Algorithms.Api and Lyo.Algorithms.Client and return the same Res types. IAlgorithmCatalog.DeleteAsync removes the definition, its revisions, and its labels. A missing key is a failed result. IAlgorithmPreview compiles an unsaved draft, converts context JSON through the revision type binding, and does not write the store. Catalog execute stays in-process. POST Algorithms/preview is the one remote run.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Algorithms.Models` (direct, lyo)
- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `DynamicExpresso.Core` `2.19.3` (direct, third-party)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (direct, microsoft)
- `System.Text.Json` `10.0.5` (direct, microsoft, netstandard2.0)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)