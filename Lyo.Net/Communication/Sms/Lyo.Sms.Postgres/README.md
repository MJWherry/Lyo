# Lyo.Sms.Postgres

**EF Core + PostgreSQL** persistence for **outbound SMS logs** (`SmsLogEntity`). This package does **not** send SMS; it wires a **`SmsDbContext`** so workers or gateways can
persist send outcomes after [`Lyo.Sms`](../Lyo.Sms/README.md) / [`Lyo.Sms.Twilio`](../Lyo.Sms.Twilio/README.md) completes.

---

## Schema & entity

- **Schema**: **`sms`** (see **`PostgresSmsOptions.Schema`** = `"sms"`).
- **`SmsLogEntity`**: **`Id`** (guid), **`To`** / **`From`**, **`Body`**, **`MediaUrlsJson`** (MMS attachments serialized), **`IsSuccess`**, **`Message`**, **`ErrorMessage`**, *
  *`ElapsedTimeMs`**, **`MessageId`**, **`Status`**, **`ErrorCode`**, timeline fields (**`DateCreated`**, **`DateSent`**, **`DateUpdated`**), **`CreatedAt`**.

Migrations ship under **`Migrations/`**; history table **`__EFMigrationsHistory`** is created inside the **`sms`** schema.

---

## Configuration: **`PostgresSmsOptions`**

| Member                                    | Meaning                                                                                                |
|-------------------------------------------|--------------------------------------------------------------------------------------------------------|
| **`SectionName`**                         | `"PostgresSms"` for **`IConfiguration`** binding                                                       |
| **`ConnectionString`**                    | Required                                                                                               |
| **`EnableAutoMigrations`**                | Honored via [`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md) migration host integration |
| Implements **`IPostgresMigrationConfig`** | Schema = **`sms`**                                                                                     |

---

## Dependency injection (**`Extensions`**)

- **`AddSmsDbContext(string connectionString)`** — registers **factory + scoped** **`SmsDbContext`** (scoped resolves a fresh context from `IDbContextFactory<SmsDbContext>`).
- **`AddSmsDbContext`** (`Action<DbContextOptionsBuilder> configure`) — classic **`AddDbContext`** path.
- **`AddSmsDbContextFactory(PostgresSmsOptions)`** / **`AddSmsDbContextFactory`** (`Action<PostgresSmsOptions>`) — singleton options + **`AddDbContextFactory`** with **`UseNpgsql`
  ** and migration history schema.
- **`AddSmsDbContextFactoryFromConfiguration(IConfiguration, section = PostgresSmsOptions.SectionName)`** — binds **`PostgresSms`** (or override) then registers factory.

Calling code still needs to **write** **`SmsLogEntity`** rows—this library only exposes the DbContext/model.

---

## When to prefer **`Lyo.Sms.Twilio.Postgres`**

If you rely on **`TwilioSmsResult`** (price, segments, account SID, direction), use [`Lyo.Sms.Twilio.Postgres`](../Lyo.Sms.Twilio.Postgres/README.md) (**`TwilioSmsLogEntity`**,
keyed by Twilio message SID).

---

## Related projects

- [`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md)
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.Sms`](../Lyo.Sms/README.md)
