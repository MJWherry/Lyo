# Lyo.Job.Postgres

PostgreSQL implementation for the job management schema using Entity Framework Core. Drop-and-play with optional auto-migrations.

## Features

- ✅ **Entity Framework Core** - Full EF Core integration with PostgreSQL
- ✅ **Auto Migrations** - Optional automatic database migrations on startup
- ✅ **Matches Existing Schema** - Schema matches `job.sql` structure (job schema)
- ✅ **DbContextFactory** - Supports `IDbContextFactory<JobContext>` for scoped operations

## Quick Start

```csharp
using Lyo.Job.Postgres;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Drop-and-play: add with auto migrations
services.AddJobDbContextFactory(new PostgresJobOptions
{
    ConnectionString = "Host=localhost;Database=postgres;Username=postgres;Password=password",
    EnableAutoMigrations = true
});

// Or with configuration binding
services.AddJobDbContextFactory(
    configuration.GetSection(PostgresJobOptions.SectionName).Get<PostgresJobOptions>()!);
```

## Design-Time Migrations

For `dotnet ef migrations add`:

```bash
export JOB_CONNECTION_STRING="Host=localhost;Database=postgres;Username=postgres;Password=password"
dotnet ef migrations add YourMigrationName --project Lyo.Job.Postgres
```

## Schema

Creates tables in the `job` schema: job_definition, job_parameter, job_trigger, job_schedule, job_run, job_run_log, job_run_parameter, job_run_result, job_file_upload, etc.

## With Lyo.Job.Api

For full job API with CRUD endpoints:

```csharp
services.AddLyoQueryServices();
services.AddFusionCache(...);  // or AddLocalCache(...)
services.AddMapster(...);  // or your Mapster config
services.AddPostgresJobManagement(new PostgresJobOptions
{
    ConnectionString = connectionString,
    EnableAutoMigrations = true
});

// After building the app:
app.BuildJobGroup();
```

## Dependencies

*(Synchronized from `Lyo.Job.Postgres.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                           | Version |
|---------------------------------------------------|---------|
| `Mapster`                                         | `[10,)` |
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder`       | `[10,)` |

### Project references

- [`Lyo.Api`](../../Api/Lyo.Api/README.md)
- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.Job.Models`](../Lyo.Job.Models/README.md)
- [`Lyo.MessageQueue.RabbitMq`](../../../Communication/MessageQueue/Lyo.MessageQueue.RabbitMq/README.md)
- [`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md)