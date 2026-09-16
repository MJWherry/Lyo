# Lyo.Recipe

Household recipes with an ingredient catalog, ordered lines and steps, hierarchical categories, and FileStorage attachment links. Listing and search stay on the Query/CRUD layer. Notes and tags use `RecipeEntityTypes`. Does not reference Favorite or Tag.

## Features

- `IRecipeStore` upserts recipes, ingredients, categories, and cooking units; replace lines and steps by id in one transaction.
- A line may link `IngredientId` or store free-text `IngredientText`, and may tag any number of steps on the same recipe.
- `ListIngredientUsageAsync` returns the recipes and step numbers that use a catalog ingredient.
- Delete guards: category with children or recipes, ingredient referenced by a line.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.EntityReference.Models` (direct, lyo)
- `Lyo.Recipe.Models` (direct, lyo)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Json` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)