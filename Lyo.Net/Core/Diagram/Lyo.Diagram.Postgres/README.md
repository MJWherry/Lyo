# Lyo.Diagram.Postgres

PostgreSQL + EF Core implementation of [`IDiagramStore`](../Lyo.Diagram.Models/README.md). Each diagram is one row in schema `diagram`, table `diagram`. The `name` column is indexed so a list does not read the cell payload. `document` is jsonb holding the pages and cells.

Register with `AddPostgresDiagramStore` or `AddPostgresDiagramStoreFromConfiguration`. The configuration section is `PostgresDiagram`. A missing section keeps the declared defaults. The context is an `IDbContextFactory<DiagramDbContext>`, opened per call.

## Features

- **PostgresDiagramStore.** `IDiagramStore` backed by `IDbContextFactory<DiagramDbContext>`.
- **JSONB document.** Pages and cells round-trip through `Lyo.Json` defaults. Id and name stay on the row.
- **Name index.** `ix_diagram_name` covers list order. Names are limited to 200 characters.

## Examples

### Registration

```csharp
services.AddPostgresDiagramStoreFromConfiguration(configuration);
```

## Database schema

PostgreSQL schema `diagram`. Table `diagram` columns: `id`, `name`, `document` (pages and cells), `created_timestamp`, `updated_timestamp`. Migrations history lives in `__EFMigrationsHistory` in the same schema.

## Migrations

At design time set `DIAGRAM_CONNECTION_STRING` and run `dotnet ef migrations add MigrationName --project Core/Diagram/Lyo.Diagram.Postgres --context DiagramDbContext`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Diagram.Models` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Json` (direct, lyo)
- `Lyo.Postgres` (direct, lyo)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)