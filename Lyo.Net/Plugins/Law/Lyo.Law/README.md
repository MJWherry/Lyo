# Lyo.Law

`ILawStore` saves jurisdictions and codes and upserts statute nodes by an opaque source key. An unchanged content hash leaves the row alone. A changed hash keeps the previous text. `LawJurisdictionKey.ThrowIfFederal` rejects `US`. Listing belongs in Lyo Query. Persistence is `Lyo.Law.Postgres`.

## Features

- `SaveJurisdictionAsync` rejects the federal key `US`.
- `UpsertNodeBySourceKeyAsync` matches jurisdiction, code, and source key.
- Citation lookup is scoped to one code.

## Examples

### Reject federal

```csharp
LawJurisdictionKey.ThrowIfFederal(key);
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Law.Models` (direct, lyo)