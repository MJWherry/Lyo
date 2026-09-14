# Lyo.Sms.Twilio.Postgres

PostgreSQL EF Core store for Twilio SMS traces: `TwilioSmsDbContext` and `TwilioSmsLogEntity`. Complements [`Lyo.Sms.Twilio`](../Lyo.Sms.Twilio/README.md). It never talks to Twilio on its own.

## `TwilioSmsLogEntity`

`Id` holds the Twilio message SID (max 34), not a random GUID. Participant fields: `To`, `From`, `Body`, `MediaUrlsJson`. Outcome: `IsSuccess`, `Message`, `ErrorMessage`, `ElapsedTimeMs`, `Status`, `ErrorCode`. Twilio fields: `NumSegments`, `AccountSid`, `Price` / `PriceUnit`, `Direction` (`MessageDirection`, defaults outbound). `DateCreated` / `DateSent` / `DateUpdated` come from the provider. `CreatedTimestamp` / `UpdatedTimestamp` track the row.

## `PostgresTwilioSmsOptions`

| Constant | Value |
| ------------- | ------------------------------------------------------------------------------------------------------------------- |
| `SectionName` | `"PostgresTwilioSms"` |
| `Schema` | `"sms"` (same schema name as [`Lyo.Sms.Postgres`](../Lyo.Sms.Postgres/README.md), a different DbContext and tables) |

`EnableAutoMigrations` follows [`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md) conventions.

## Service registration

- `AddTwilioSmsDbContext(string connectionString)` wires a factory plus a scoped `TwilioSmsDbContext`.
- `AddTwilioSmsDbContextFactory(PostgresTwilioSmsOptions)` wires singleton options, `AddDbContextFactory`, and `UseNpgsql` with the `sms.__EFMigrationsHistory` history table (schema `sms`).
- `AddTwilioSmsDbContextFactoryFromConfiguration(IConfiguration, section = PostgresTwilioSmsOptions.SectionName)` binds config, then registers the factory.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Postgres` (direct, lyo)
- `Lyo.Sms.Twilio` (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Sms` (transitive, lyo)
- `Lyo.Sms.Models` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Twilio` `7.14.9` (transitive, third-party)