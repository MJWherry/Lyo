# Lyo.Tag.Seed

`TagEfSeedContributor` writes `options.Count` public tags (type `tag`, unique slug) for tenant `TagEfSeedContributor.TenantId` and subject `seed` / `seed`. `OnClear` deletes tags so `--replace` can run.

## Features

- **Fixed subject.** Every row uses the same tenant, subject, and actor.
- **Replace.** `OnClear` deletes tags.

## Examples

### Registration

```csharp
services.AddLyoSeed();
services.AddSeedContributor<TagEfSeedContributor>();
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.EntityReference.Models` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Seed` (direct, lyo)
- `Lyo.Tag.Postgres` (direct, lyo)
- `Bogus` `35.6.5` (direct, third-party)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (direct, third-party)
- `Lyo.Api.Client` (transitive, lyo)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.EntityReference.Postgres` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.Http.Client` (transitive, lyo)
- `Lyo.Json` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Postgres` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Tag` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Npgsql` `10.0.3` (transitive, third-party)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft)