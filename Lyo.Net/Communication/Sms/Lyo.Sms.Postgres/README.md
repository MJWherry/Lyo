# Lyo.Sms.Postgres

**EF Core + PostgreSQL** persistence for **outbound SMS logs** (`SmsLogEntity`). This package does **not** send SMS; it wires a **`SmsDbContext`** so workers or gateways can persist send outcomes after [`Lyo.Sms`](../Lyo.Sms/README.md) / [`Lyo.Sms.Twilio`](../Lyo.Sms.Twilio/README.md) completes.

---

## Schema & entity

- **Schema**: **`sms`** (see **`PostgresSmsOptions.Schema`** = `"sms"`).
- **`SmsLogEntity`**: **`Id`** (guid), **`To`** / **`From`**, **`Body`**, **`MediaUrlsJson`** (MMS attachments serialized), **`IsSuccess`**, **`Message`**, **`ErrorMessage`**, * *`ElapsedTimeMs`**, **`MessageId`**, **`Status`**, **`ErrorCode`**, timeline fields (**`DateCreated`**, **`DateSent`**, **`DateUpdated`**), **`CreatedAt`**.

## Configuration: **`PostgresSmsOptions`**

| Member | Meaning |
| ----------------------------------------- | ------------------------------------------------------------------------------------------------------ |
| **`SectionName`** | `"PostgresSms"` for **`IConfiguration`** binding |
| **`ConnectionString`** | Required |
| **`EnableAutoMigrations`** | Honored via [`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md) migration host integration |
| Implements **`IPostgresMigrationConfig`** | Schema = **`sms`** |

---

## Dependency injection (**`Extensions`**)

- **`AddSmsDbContext(string connectionString)`** — registers **factory + scoped** **`SmsDbContext`** (scoped resolves a fresh context from `IDbContextFactory<SmsDbContext>`).
- **`AddSmsDbContext`** (`Action<DbContextOptionsBuilder> configure`) — classic **`AddDbContext`** path.
- **`AddSmsDbContextFactory(PostgresSmsOptions)`** / **`AddSmsDbContextFactory`** (`Action<PostgresSmsOptions>`) — singleton options + **`AddDbContextFactory`** with **`UseNpgsql` ** and migration history schema.
- **`AddSmsDbContextFactoryFromConfiguration(IConfiguration, section = PostgresSmsOptions.SectionName)`** — binds **`PostgresSms`** (or override) then registers factory.

## When to prefer **`Lyo.Sms.Twilio.Postgres`**

If you rely on **`TwilioSmsResult`** (price, segments, account SID, direction), use [`Lyo.Sms.Twilio.Postgres`](../Lyo.Sms.Twilio.Postgres/README.md) (**`TwilioSmsLogEntity`**, keyed by Twilio message SID). ---

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Postgres` — (direct, lyo)
- `Lyo.Sms` — (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (direct, microsoft)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.Metrics` — (transitive, lyo)
- `Lyo.Result` — (transitive, lyo)
- `Lyo.Sms.Models` — (transitive, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` — (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` — (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` — (transitive, third-party)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)