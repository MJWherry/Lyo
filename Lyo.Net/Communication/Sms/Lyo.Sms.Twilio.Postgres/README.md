# Lyo.Sms.Twilio.Postgres

EF Core PostgreSQL persistence tailored for **Twilio-outbound (+ metadata)** traces: **`TwilioSmsDbContext`** and **`TwilioSmsLogEntity`**. Complements [ `Lyo.Sms.Twilio`](../Lyo.Sms.Twilio/README.md); it never calls Twilio by itself.

---

## **`TwilioSmsLogEntity`** (high level)

Keyed by **`Id`** — the **Twilio message SID** (max 34), not a random GUID: - Participant fields: **`To`**, **`From`**, **`Body`**, **`MediaUrlsJson`**. - Outcome: **`IsSuccess`**, **`Message`**, **`ErrorMessage`**, **`ElapsedTimeMs`**, **`Status`**, **`ErrorCode`**. - Twilio-specific: **`NumSegments`**, **`AccountSid`**, **`Price`** / **`PriceUnit`**, **`Direction`** (**`MessageDirection`**, defaults outbound). Timestamps **`DateCreated`** / **`DateSent`** / **`DateUpdated`** mirror provider metadata; **`CreatedTimestamp`** / **`UpdatedTimestamp`** track row lifecycle. ---

## Configuration: **`PostgresTwilioSmsOptions`**

| Constant | Value |
| ----------------- | ---------------------------------------------------------------------------------------------------------------------------------------------- |
| **`SectionName`** | `"PostgresTwilioSms"` |
| **`Schema`** | `"sms"` (shared schema name with [`Lyo.Sms.Postgres`](../Lyo.Sms.Postgres/README.md)—**different DbContext/table set**, same PG schema budget) |

**`EnableAutoMigrations`** follows [`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md) conventions.

---

## Dependency injection (**`Extensions`**)

- **`AddTwilioSmsDbContext(string connectionString)`** — factory + scoped **`TwilioSmsDbContext`**.
- **`AddTwilioSmsDbContextFactory(PostgresTwilioSmsOptions)`** — singleton options + **`AddDbContextFactory`** + **`UseNpgsql`** with **`sms.__EFMigrationsHistory`** history table (schema **`sms`**).
- **`AddTwilioSmsDbContextFactoryFromConfiguration(IConfiguration, section = PostgresTwilioSmsOptions.SectionName)`** — binds config then registers factory.
- **`AddTwilioSmsDbContext`** (`Action<DbContextOptionsBuilder>`) — direct **`AddDbContext`**.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Postgres` — (direct, lyo)
- `Lyo.Sms.Twilio` — (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (direct, microsoft)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.Metrics` — (transitive, lyo)
- `Lyo.Result` — (transitive, lyo)
- `Lyo.Sms` — (transitive, lyo)
- `Lyo.Sms.Models` — (transitive, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` — (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` — (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` — (transitive, third-party)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `Twilio` `7.14.9` — (transitive, third-party)