# Lyo.Algorithms

Compiles named C# algorithms and DynamicExpresso expressions once, then runs them through IAlgorithmRegistry. After the first compile, Execute is a delegate call with no database and no Task. AddMemoryAlgorithmStore keeps the catalog in process. Postgres and SQLite are optional packages that implement the same store. Catalog rows are the Req and Res types in Lyo.Algorithms.Models. Runtime rows do not replace a startup key unless AllowRuntimeOverride is set.

## Features

- **Startup store.** Named C# via AddNamedAlgorithm and bag expressions via AlgorithmOptions.Expressions or AddExpressionAlgorithm. Compiled at registration, then frozen.
- **Runtime catalog.** IAlgorithmDefinitionStore takes Req and returns Res. It appends revisions, copies a snapshot on revert, and retargets mutable labels. Immutable labels stay put. Structured kinds store a JSON payload.
- **AlgorithmRef.** Latest, Revision, or Label. Latest and mutable labels follow new revisions. Pins and immutable labels do not.
- **Math expressions.** Math and Convert only (Abs, Min, Max, Round, Floor, Ceiling, Pow, Sqrt, Sign, and a few Convert.To* methods). Algo("key") and score("key") call another catalog algorithm. Other methods, member access, and new of non-primitive types are denied.
- **Context bag.** `AlgorithmContextBag` stores identifiers on a case-insensitive `ContextBag`. `Set` rejects dotted names. A factory runs once when `ReadDouble` or `TryGetAs<T>` reads the key. A factory that receives the bag can read sibling identifiers.
- **Scorers.** DiscreteMap, RangeBand, Linear, and Composite (Sum, WeightedSum, or Formula). ExecuteWithBreakdownAsync returns the value plus nested steps. Execute does not.

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

AddMemoryAlgorithmStore is in this package and needs no database. Postgres and SQLite are Lyo.Algorithms.Postgres and Lyo.Algorithms.Sqlite. Call one store after AddAlgorithms. Hosts that only need startup C# can skip a store; resolving IAlgorithmCatalog without a store throws. HTTP catalog routes live in Lyo.Algorithms.Api and Lyo.Algorithms.Client and return the same Res types. Execute stays in-process.

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