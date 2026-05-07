# Lyo.Sms.Twilio.Postgres

EF Core PostgreSQL persistence tailored for **Twilio-outbound (+ metadata)** traces: **`TwilioSmsDbContext`** and **`TwilioSmsLogEntity`**. Complements [
`Lyo.Sms.Twilio`](../Lyo.Sms.Twilio/README.md); it never calls Twilio by itself.

---

## **`TwilioSmsLogEntity`** (high level)

Keyed by **`Id`** — the **Twilio message SID** (max 34), not a random GUID:

- Participant fields: **`To`**, **`From`**, **`Body`**, **`MediaUrlsJson`**.
- Outcome: **`IsSuccess`**, **`Message`**, **`ErrorMessage`**, **`ElapsedTimeMs`**, **`Status`**, **`ErrorCode`**.
- Twilio-specific: **`NumSegments`**, **`AccountSid`**, **`Price`** / **`PriceUnit`**, **`Direction`** (**`MessageDirection`**, defaults outbound).

Timestamps **`DateCreated`** / **`DateSent`** / **`DateUpdated`** mirror provider metadata; **`CreatedTimestamp`** / **`UpdatedTimestamp`** track row lifecycle.

---

## Configuration: **`PostgresTwilioSmsOptions`**

| Constant          | Value                                                                                                                                          |
|-------------------|------------------------------------------------------------------------------------------------------------------------------------------------|
| **`SectionName`** | `"PostgresTwilioSms"`                                                                                                                          |
| **`Schema`**      | `"sms"` (shared schema name with [`Lyo.Sms.Postgres`](../Lyo.Sms.Postgres/README.md)—**different DbContext/table set**, same PG schema budget) |

**`EnableAutoMigrations`** follows [`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md) conventions.

---

## Dependency injection (**`Extensions`**)

Same pattern as the provider-neutral Postgres package:

- **`AddTwilioSmsDbContext(string connectionString)`** — factory + scoped **`TwilioSmsDbContext`**.
- **`AddTwilioSmsDbContextFactory(PostgresTwilioSmsOptions)`** — singleton options + **`AddDbContextFactory`** + **`UseNpgsql`** with **`sms.__EFMigrationsHistory`** history
  table (schema **`sms`**).
- **`AddTwilioSmsDbContextFactoryFromConfiguration(IConfiguration, section = PostgresTwilioSmsOptions.SectionName)`** — binds config then registers factory.
- **`AddTwilioSmsDbContext`** (`Action<DbContextOptionsBuilder>`) — direct **`AddDbContext`**.

Persist rows from **`TwilioSmsService`** pipelines (decorate/wrap **`MessageSent`** / **`BulkSent`** handlers or central middleware) depending on app architecture.

---

## Related projects

- [`Lyo.Sms.Postgres`](../Lyo.Sms.Postgres/README.md) — generic **`SmsLogEntity`**
- [`Lyo.Sms.Twilio`](../Lyo.Sms.Twilio/README.md)
- [`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md)
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
