# Lyo.Job.Seed

`JobEfSeedContributor` writes `options.Count` disabled job definitions, each with a parameter, a disabled schedule and schedule parameter, a parallel restriction, and a historical succeeded run (log, run parameter, and run result). It also writes one disabled blackout calendar and window, one stopped worker, one file-upload row, and one disabled workflow with a finished workflow run. Definitions, schedules, and workflow steps stay disabled so seeding does not start workers. `OnClear` deletes run and workflow children before definitions so `--replace` can run.

## Features

- **Disabled.** Definitions and schedules are not enabled.
- **Replace.** `OnClear` deletes runs, workflows, schedules, parameters, then definitions.

## Examples

### Registration

```csharp
services.AddLyoSeed();
services.AddSeedContributor<JobEfSeedContributor>();
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Job.Postgres` (direct, lyo)
- `Lyo.Seed` (direct, lyo)
- `Bogus` `35.6.5` (direct, third-party)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (direct, third-party)
- `Lyo.Api` (transitive, lyo)
- `Lyo.Api.Client` (transitive, lyo)
- `Lyo.Api.Export` (transitive, lyo)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Audit` (transitive, lyo)
- `Lyo.Cache` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Compression` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Diagnostic.AspNetCore` (transitive, lyo)
- `Lyo.Diff` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.EntityReference.Models` (transitive, lyo)
- `Lyo.Formatter` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.Http.Client` (transitive, lyo)
- `Lyo.Job.Models` (transitive, lyo)
- `Lyo.Json` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.MessageQueue` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Postgres` (transitive, lyo)
- `Lyo.Query` (transitive, lyo)
- `Lyo.Query.Evaluation` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Query.Sql` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Schedule.Models` (transitive, lyo)
- `Lyo.Scheduler` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Lyo.Validation.Models` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
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
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Npgsql` `10.0.3` (transitive, third-party)
- `SmartFormat.NET` `3.6.1` (transitive, third-party)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.Diagnostics.DiagnosticSource` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)