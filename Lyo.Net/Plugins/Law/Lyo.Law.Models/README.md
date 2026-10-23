# Lyo.Law.Models

Wire types for the state statute API. Jurisdictions are US states. A code is one named compilation inside a state. Nodes are the outline and the text. This package has no DI
registration. Federal law is not a type in this package.

## Features

- Enums for node kind and annotation kind. Node text is stored as text.
- `Constants.Rest.Law` route prefixes, including the source-key upsert and citation lookup.

## Examples

### Route constants

```csharp
var nodes = Constants.Rest.Law.Nodes;
var upsert = Constants.Rest.Law.NodesBySource;
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)