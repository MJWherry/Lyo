# Lyo.Algorithms.Api

Maps catalog list, get, revision append, revert, label, delete, and preview routes. Writes go through `IAlgorithmCatalog` so syntax and startup-key checks still run. Reads return the store's `*Res` types with no second mapping. `Kind` is `AlgorithmKind`. Revert takes `AlgorithmRevertReq`. `DELETE Algorithms/{key}` removes the definition, its revisions, and its labels. `POST Algorithms/preview` runs an unsaved draft and does not append a revision. Catalog execute stays in-process. Register with `AddAlgorithmsApiFromConfiguration` (section `PostgresAlgorithms`), then call `BuildAlgorithmsGroup`.

## Features

- `BuildAlgorithmsGroup` maps GET /Algorithms, GET by key, revisions, revert, labels, DELETE by key, and POST /Algorithms/preview.
- `POST Algorithms/preview` converts the context JSON with the revision type binding and returns `value`, the result alias, and the step breakdown.
- POST revisions and revert, and PUT labels, return `AlgorithmDefinitionRes` or `AlgorithmLabelRes`. Failed catalog results are 400.
- `AddAlgorithmsApiFromConfiguration` registers the Postgres store and `AddAlgorithms`.

## Examples

### Map the Algorithms API

```csharp
services.AddAlgorithmsApiFromConfiguration(configuration);
var app = builder.Build();
app.BuildAlgorithmsGroup();
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Algorithms` (direct, lyo)
- `Lyo.Algorithms.Models` (direct, lyo)
- `Lyo.Algorithms.Postgres` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.Postgres` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `DynamicExpresso.Core` `2.19.3` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)