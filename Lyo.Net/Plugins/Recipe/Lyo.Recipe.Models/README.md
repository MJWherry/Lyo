# Lyo.Recipe.Models

Wire types for the household recipe API. `Request/` and `Response/` types are used by `Lyo.Recipe.Api` and `Lyo.Recipe.Web.Components`. Enums live here so the store contract and HTTP surface share one vocabulary. This package has no DI registration.

## Features

- Enums for status, difficulty, and attachment role.
- `Constants.Rest.Recipe` route prefixes.
- `RecipeFilePaths` FileStorage PathPrefix helpers (`recipe/recipes/{id}`, `recipe/steps/{id}`).
- `RecipeEntityTypes` stable EntityRef type names for Note/Tag composition.

## Examples

### Route constants

```csharp
var recipes = Constants.Rest.Recipe.Recipes;
var prefix = RecipeFilePaths.ForRecipe(recipeId);
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