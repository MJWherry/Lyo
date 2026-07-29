# Lyo.Email.Postgres

PostgreSQL schema and `EmailDbContext` for logging emails sent by [`Lyo.Email`](../Lyo.Email/README.md).
This package does **not** subscribe to `EmailService` events — consumers handle the mapping and
insertion (e.g. from the `EmailSent` / `BulkEmailSent` events) into the tables exposed here.

## Examples

### DI extensions

```csharp
services.AddEmailDbContextFactoryFromConfiguration(builder.Configuration);
// or
services.AddEmailDbContextFactory(opts =>
{
    opts.ConnectionString = "Host=...;Database=...;Username=...;Password=...";
    opts.EnableAutoMigrations = true;
});
```

## What ships

- `EmailDbContext` — EF Core context with `DbSet<EmailLogEntity>` (`email_logs`) and `DbSet<EmailAttachmentLogEntity>` (`email_attachment_logs`). Default schema is `email`. `SaveChanges` sets `CreatedTimestamp` on insert and `UpdatedTimestamp` on update for both entities.
- `EmailDbContextFactory` — design-time factory used by the EF Core tooling.
- `EmailLogEntity` — sender, recipients (as JSON), subject, success flag, SMTP message, error message, provider `MessageId`, and audit timestamps. Attachment bytes are not stored here.
- `EmailAttachmentLogEntity` — attachment metadata only (file name, optional `FileStorageId`, `TemplateId`, `ContentType`, `MetadataJson`, `SortOrder`) keyed back to an `EmailLogEntity`.
- `PostgresEmailOptions` — `IPostgresMigrationConfig` implementation. Section name is `PostgresEmail`; default schema is `email`; `EnableAutoMigrations` defaults to `false`.

## DI extensions

All extensions hang off `IServiceCollection`:

| Extension | Description |
| -------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `AddEmailDbContext(string connectionString)` | Registers an `IDbContextFactory<EmailDbContext>` plus a scoped `EmailDbContext` resolved from the factory. |
| `AddEmailDbContext(Action<DbContextOptionsBuilder> configure)` | Registers `EmailDbContext` using a caller-supplied options builder (no factory). |
| `AddEmailDbContextFactory(PostgresEmailOptions options)` | Registers `IDbContextFactory<EmailDbContext>` against `options.ConnectionString` with the `email` migrations history schema, plus `Lyo.Postgres` migration runner support. |
| `AddEmailDbContextFactory(Action<PostgresEmailOptions> configure)` | Same as above with an inline configuration callback. |
| `AddEmailDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresEmailOptions.SectionName)` | Binds `PostgresEmailOptions` from configuration (section defaults to `PostgresEmail`) and registers the factory. |

## Target frameworks

`netstandard2.0;net10.0`

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Postgres` — (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (direct, microsoft)
- `Microsoft.EntityFrameworkCore` `10.0.5` — (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` — (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` — (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` — (transitive, third-party)