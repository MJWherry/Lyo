# Lyo.Formatter.Algorithms

Registers an IFormatterExpressionEvaluator that understands Algo("key") and score("key") by calling IAlgorithmRegistry. Lyo.Formatter does not reference Algorithms. Job, Api, and AddFormatterService stay unchanged until a host calls AddFormatterAlgorithms().

## Features

- **Opt-in.** AddFormatterAlgorithms after AddAlgorithms and AddFormatterService. Hosts that skip it keep the built-in DateTime and LINQ expressions.
- **Latest key.** The token is the whole expression: {Algo("attack.physical")} or {score("fish.score")}. Pins and labels stay on AlgorithmRef in application code.
- **Same registry.** A cold key is loaded once. Later formats hit the compiled delegate.

## Examples

### Format a catalog algorithm

```csharp
services.AddAlgorithms();
services.AddMemoryAlgorithmStore();
services.AddFormatterService();
services.AddFormatterAlgorithms();

var text = formatter.Format("{Algo(\"attack.physical\")}", new Dictionary<string, object?> { ["Atk"] = 10d });
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Algorithms` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Formatter` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Algorithms.Models` (transitive, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `DynamicExpresso.Core` `2.19.3` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `SmartFormat.NET` `3.6.1` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)