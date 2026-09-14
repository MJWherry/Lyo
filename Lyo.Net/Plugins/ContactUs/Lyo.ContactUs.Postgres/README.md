# Lyo.ContactUs.Postgres

PostgreSQL and EF Core implementation of [`Lyo.ContactUs.IContactUsService`](../Lyo.ContactUs/README.md) (`PostgresContactUsService`) through `ContactUsDbContext`, `PostgresContactUsOptions`, and `AddPostgresMigrations<ContactUsDbContext, PostgresContactUsOptions>` so the host can apply schema upgrades on startup.

## Package contents

- `ContactUsDbContext` + `ContactUsDbContextFactory` under `Database/`, with a single `DbSet<ContactSubmissionEntity>` (name / email / subject / message / phone / company / created timestamp; lengths match `ContactUsRequest`).
- `PostgresContactUsService`. A `ContactUsServiceBase` subclass that persists each submission through the registered `IDbContextFactory<ContactUsDbContext>`, logs the new id, and implements `TestConnectionAsync` by issuing `Database.CanConnectAsync(...)`.
- `PostgresContactUsOptions` (`SectionName = "PostgresContactUs"`, `Schema = "contact"`, `ConnectionString`, `EnableAutoMigrations`).
- `InitialCreate` migration under `Migrations/`.

## Service registration (`Extensions`)

| Entry point | Effect |
| ----------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `AddContactUsDbContext(string connectionString)` | Registers the DbContext factory plus a scoped `ContactUsDbContext` resolved from the factory. Useful for ad-hoc EF code. |
| `AddContactUsDbContextFactory(Action<PostgresContactUsOptions>)` *(plus `(options)` and `FromConfiguration` overloads)* | Registers `IOptions<PostgresContactUsOptions>`, `AddPostgresMigrations<ContactUsDbContext, PostgresContactUsOptions>()`, and `IDbContextFactory<ContactUsDbContext>` (`UseNpgsql` + migrations history under the `contact` schema). |
| `AddContactUsPostgres(Action<PostgresContactUsOptions>)` *(plus `(options)` and `FromConfiguration` overloads)* | Calls `AddContactUsDbContextFactory(...)`, ensures a `ContactUsServiceOptions` singleton exists (defaults if not provided), and registers `IContactUsService` → scoped `PostgresContactUsService`. |

`AddContactUsPostgres(...)` is the usual one-shot registration. Use `AddContactUsDbContextFactory(...)` plus your own service registration when you want to wrap or decorate the service yourself.

## Database schema

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
- `Microsoft.Extensions.Options` `10.0.5` (direct, microsoft)
- `Lyo.Health` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)