# Lyo.ContactUs.Postgres

PostgreSQL + EF Core implementation of [`Lyo.ContactUs.IContactUsService`](../Lyo.ContactUs/README.md) (`PostgresContactUsService`) via `ContactUsDbContext`, `PostgresContactUsOptions`, and `AddPostgresMigrations<ContactUsDbContext, PostgresContactUsOptions>` so the host can apply schema upgrades on startup.

## What ships

- `ContactUsDbContext` + `ContactUsDbContextFactory` under `Database/`, with a single `DbSet<ContactSubmissionEntity>` (name / email / subject / message / phone / company / created timestamp; lengths mirror `ContactUsRequest`).
- `PostgresContactUsService`. `ContactUsServiceBase` subclass that persists each submission via the registered `IDbContextFactory<ContactUsDbContext>`, logs the new id, and implements `TestConnectionAsync` by issuing `Database.CanConnectAsync(...)`.
- `PostgresContactUsOptions` (`SectionName = "PostgresContactUs"`, `Schema = "contact"`, `ConnectionString`, `EnableAutoMigrations`).
- `InitialCreate` migration under `Migrations/`.

## DI registration (`Extensions`)

| Entry point | What it does |
| ----------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `AddContactUsDbContext(string connectionString)` | Registers the DbContext factory plus a scoped `ContactUsDbContext` resolved from the factory (handy for ad-hoc EF code). |
| `AddContactUsDbContext(Action<DbContextOptionsBuilder>)` | Bare `AddDbContext<ContactUsDbContext>(...)` bring your own provider configuration. |
| `AddContactUsDbContextFactory(Action<PostgresContactUsOptions>)` *(plus `(options)` and `FromConfiguration` overloads)* | Registers `IOptions<PostgresContactUsOptions>`, `AddPostgresMigrations<ContactUsDbContext, PostgresContactUsOptions>()`, and `IDbContextFactory<ContactUsDbContext>` (`UseNpgsql` + migrations history under the `contact` schema). |
| `AddContactUsPostgres(Action<PostgresContactUsOptions>)` *(plus `(options)` and `FromConfiguration` overloads)* | Calls `AddContactUsDbContextFactory(...)`, ensures a `ContactUsServiceOptions` singleton exists (defaults if not provided), and registers `IContactUsService` → scoped `PostgresContactUsService`. |

`AddContactUsPostgres(...)` is the one-stop registration for most callers. Use `AddContactUsDbContextFactory(...)` plus your own service registration when you want to wrap or
decorate the service yourself.

## Schema

| Column | Source |
| --------------------------------------------------------- | -------------------------------------------------------- |
| `Id` *(PK, `Guid`)* | Generated in `PostgresContactUsService.SubmitCoreAsync`. |
| `Name`, `Email`, `Subject`, `Message`, `Phone`, `Company` | Copied verbatim from the validated `ContactUsRequest`. |
| `CreatedTimestamp` | `DateTime.UtcNow` at insert time. |

Migrations history is tracked in `__EFMigrationsHistory` under the `contact` schema configured on `PostgresContactUsOptions.Schema`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.ContactUs` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Postgres` (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (direct, microsoft)
- `Lyo.Common` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)