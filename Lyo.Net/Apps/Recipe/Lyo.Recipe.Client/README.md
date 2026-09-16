# Lyo.Recipe.Client

`IRecipeClient` wraps ingredient-line and step GET/PUT, nested notes/tags, and multipart attachment upload. Category, ingredient, recipe, and attachment CRUD stay on `IApiClient`. Register with `AddRecipeClient<TApiClient>` or a factory overload. `RecipeClientOptions.RoutePrefix` is a path prefix, not an absolute URL.

## Features

- `ReplaceIngredientLinesAsync` / `ReplaceStepsAsync` PUT the nested collection routes and round-trip row ids.
- `ListUnitsAsync` / `SaveUnitAsync` use `Recipe/Units`. `ListIngredientUsageAsync` GETs `Recipe/Ingredients/{id}/Usage`.
- `AddNoteAsync` / `AddTagAsync` take a parent route (`Recipe/Recipes`, `Recipe/Ingredients`, or `Recipe/Categories`).
- `UploadAttachmentAsync` posts `Recipe/Attachments/upload` with extra form fields and no content-type restriction.

## Examples

### Register on an existing IApiClient

```csharp
services.AddRecipeClient<ApiClient>();
var lines = await recipe.ReplaceIngredientLinesAsync(recipeId, lines);
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Client` (direct, lyo)
- `Lyo.Http.Client` (direct, lyo)
- `Lyo.Note` (direct, lyo)
- `Lyo.Recipe.Models` (direct, lyo)
- `Lyo.Tag` (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (direct, microsoft)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.EntityReference.Models` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Json` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft)