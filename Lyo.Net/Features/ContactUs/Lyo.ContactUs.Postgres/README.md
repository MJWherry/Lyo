# Lyo.ContactUs.Postgres

PostgreSQL + EF Core implementation of [`Lyo.ContactUs.IContactUsService`](../Lyo.ContactUs/README.md) (`PostgresContactUsService`) backed by `ContactUsDbContext`,
`PostgresContactUsOptions`, and `AddPostgresMigrations<ContactUsDbContext, PostgresContactUsOptions>` for auto-applied schema upgrades on host startup.

## What ships

- `ContactUsDbContext` + `ContactUsDbContextFactory` under `Database/`, with a single `DbSet<ContactSubmissionEntity>` (name / email / subject / message / phone / company /
  created timestamp; lengths mirror `ContactUsRequest`).
- `PostgresContactUsService` — `ContactUsServiceBase` subclass that persists each submission via the registered `IDbContextFactory<ContactUsDbContext>`, logs the new id, and
  implements `TestConnectionAsync` by issuing `Database.CanConnectAsync(...)`.
- `PostgresContactUsOptions` (`SectionName = "PostgresContactUs"`, `Schema = "contact"`, `ConnectionString`, `EnableAutoMigrations`).
- `InitialCreate` migration under `Migrations/`.

## DI registration (`Extensions`)

| Entry point                                                                                                             | What it does                                                                                                                                                                                                                        |
|-------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `AddContactUsDbContext(string connectionString)`                                                                        | Registers the DbContext factory plus a scoped `ContactUsDbContext` resolved from the factory (handy for ad-hoc EF code).                                                                                                            |
| `AddContactUsDbContext(Action<DbContextOptionsBuilder>)`                                                                | Bare `AddDbContext<ContactUsDbContext>(...)` — bring your own provider configuration.                                                                                                                                               |
| `AddContactUsDbContextFactory(Action<PostgresContactUsOptions>)` *(plus `(options)` and `FromConfiguration` overloads)* | Registers `IOptions<PostgresContactUsOptions>`, `AddPostgresMigrations<ContactUsDbContext, PostgresContactUsOptions>()`, and `IDbContextFactory<ContactUsDbContext>` (`UseNpgsql` + migrations history under the `contact` schema). |
| `AddContactUsPostgres(Action<PostgresContactUsOptions>)` *(plus `(options)` and `FromConfiguration` overloads)*         | Calls `AddContactUsDbContextFactory(...)`, ensures a `ContactUsServiceOptions` singleton exists (defaults if not provided), and registers `IContactUsService` → scoped `PostgresContactUsService`.                                  |

`AddContactUsPostgres(...)` is the one-stop registration for most callers. Use `AddContactUsDbContextFactory(...)` plus your own service registration when you want to wrap or
decorate the service yourself.

## Schema

| Column                                                    | Source                                                   |
|-----------------------------------------------------------|----------------------------------------------------------|
| `Id` *(PK, `Guid`)*                                       | Generated in `PostgresContactUsService.SubmitCoreAsync`. |
| `Name`, `Email`, `Subject`, `Message`, `Phone`, `Company` | Copied verbatim from the validated `ContactUsRequest`.   |
| `CreatedTimestamp`                                        | `DateTime.UtcNow` at insert time.                        |

Migrations history is tracked in `__EFMigrationsHistory` under the `contact` schema configured on `PostgresContactUsOptions.Schema`.

## Dependencies

*(Synchronized from `Lyo.ContactUs.Postgres.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                           | Version |
|---------------------------------------------------|---------|
| `Microsoft.EntityFrameworkCore.Design`            | `[10,)` |
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder`       | `[10,)` |
| `Microsoft.Extensions.Options`                    | `[10,)` |

### Project references

- [`Lyo.ContactUs`](../Lyo.ContactUs/README.md)
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md)
