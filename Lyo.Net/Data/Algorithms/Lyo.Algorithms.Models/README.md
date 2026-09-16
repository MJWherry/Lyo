# Lyo.Algorithms.Models

The catalog contract shared by the in-memory store, Postgres, SQLite, the API, and the client. `Request/` and `Response/` are separate: a save sends `AlgorithmRevisionReq`, a read returns `AlgorithmDefinitionRes`. `Kind` is `AlgorithmKind`, not a string. The compiler and registry stay in `Lyo.Algorithms`. This package has no DI registration.

## Features

- `AlgorithmKind` for expression, named, map, band, linear, and composite revisions.
- `AlgorithmDefinitionReq` / `AlgorithmDefinitionRes` for definition metadata and the current revision.
- `AlgorithmRevisionReq` / `AlgorithmRevisionRes` for append. `AlgorithmRevertReq` copies an existing snapshot.
- `AlgorithmLabelReq` / `AlgorithmLabelRes` for mutable and immutable labels.
- `Constants.Rest.Algorithms` route prefixes shared by the API and the client.

## Examples

### Route constants

```csharp
var revisions = Constants.Rest.Algorithms.Revisions("attack.physical");
```