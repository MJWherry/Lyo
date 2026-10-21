# Lyo.HomeInventory.Models

Shared wire types for household inventory. `Request/` and `Response/` types are used by `Lyo.HomeInventory.Api` (`CreateBuilder` + stock/notes/tags routes) and
`Lyo.HomeInventory.Web.Components`. Enums live here so the store contract (`Lyo.HomeInventory`) and HTTP surface share one vocabulary. This package has no DI registration.

## Features

- Enums for item status, condition, movement type, and attachment role.
- `HomeProductKeys` known identifier kinds and `HomeProductKeyEntry` (kind/value/note). The same kind may repeat.
- `Constants.Rest.HomeInventory` route prefixes.
- `HomeInventoryFilePaths` FileStorage PathPrefix helpers (`home-inventory/items/{id}`).
- `HomeInventoryEntityTypes` stable EntityRef type names for Note/Tag composition.

## Examples

### Route constants

```csharp
var items = Constants.Rest.HomeInventory.Items;
var prefix = HomeInventoryFilePaths.ForItem(itemId);
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Models` (direct, lyo)
- `Lyo.EntityReference.Models` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Json` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)