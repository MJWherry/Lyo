# Lyo.Recipe.Api

Maps Recipe Req/Res CRUD via `Lyo.Api` `CreateBuilder` (categories, ingredients, recipes, attachments) plus ingredient-line and step GET/PUT, nested notes/tags, and optional
multipart upload when `IRecipeAttachmentFiles` is registered. Register with `AddRecipeApiFromConfiguration`, then call `BuildRecipeGroup`. Example host: `Lyo.Recipe.Api.Example`.

## Features

- `BuildRecipeGroup` maps Req/Res CRUD for categories, ingredients, recipes, and attachments.
- Ingredient lines and steps under `Recipe/Recipes/{id}/IngredientLines` and `…/Steps` (GET list, PUT replace, ids preserved). `GET/PUT/DELETE Recipe/Units` and
  `GET Recipe/Ingredients/{id}/Usage`.
- Nested `Notes` and `Tags` on categories, ingredients, and recipes; delete cascades those rows.
- `POST Recipe/Attachments/upload` accepts any file role (`Document`, `Scan`, `Card`, `Image`, `Other`) and returns 501 when the FileStorage helper is missing.
- `AddRecipeApiFromConfiguration` registers the Postgres store, Note/Tag stores on the same connection, and CRUD services.

## Examples

### Map the Recipe API

```csharp
services.AddLocalCache();
services.AddLyoQueryServices();
services.AddRecipeApiFromConfiguration(configuration);
var app = builder.Build();
app.BuildRecipeGroup();
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api` (direct, lyo)
- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Note` (direct, lyo)
- `Lyo.Note.Postgres` (direct, lyo)
- `Lyo.Recipe` (direct, lyo)
- `Lyo.Recipe.FileStorage` (direct, lyo)
- `Lyo.Recipe.Models` (direct, lyo)
- `Lyo.Recipe.Postgres` (direct, lyo)
- `Lyo.Tag` (direct, lyo)
- `Lyo.Tag.Postgres` (direct, lyo)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Cache` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Compression` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Diagnostic.AspNetCore` (transitive, lyo)
- `Lyo.Diff` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.EntityReference.Models` (transitive, lyo)
- `Lyo.EntityReference.Postgres` (transitive, lyo)
- `Lyo.FileMetadataStore` (transitive, lyo)
- `Lyo.FileStorage` (transitive, lyo)
- `Lyo.Formatter` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.IO.FileSystem` (transitive, lyo)
- `Lyo.IO.Temp` (transitive, lyo)
- `Lyo.Json` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Postgres` (transitive, lyo)
- `Lyo.Query` (transitive, lyo)
- `Lyo.Query.Evaluation` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Query.Sql` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Search.Postgres` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Lyo.Validation.Models` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `DynamicExpresso.Core` `2.19.3` (transitive, third-party)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.AspNetCore.OpenApi` `10.0.5` (transitive, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Analyzers` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.DataAnnotations` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `SmartFormat.NET` `3.6.1` (transitive, third-party)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)