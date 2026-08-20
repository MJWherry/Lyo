# Lyo.Sms.Models

Shared types for [`Lyo.Sms`](../Lyo.Sms/README.md): payloads, paging, events, normalization, and base options. This package does not send SMS. Implementations live in provider packages (`Lyo.Sms.Twilio`, and others).

## `SmsRequest`

Canonical shape for outbound SMS/MMS. `To` / `From` prefer E.164. Builders and services normalize many US-centric inputs. `Body` is text. Combined length is validated against `SmsServiceOptions.MaxMessageBodyLength` in the core library. `MediaUrls` is a `List<Uri>` for MMS attachments (empty for plain SMS). `ToString()` truncates bodies for `DebuggerDisplay`.

## `SmsMessageQueryFilter` / `SmsMessageQueryResults<T>`

Cursor-based listing used by `ISmsService.GetMessagesAsync`:

| Field | Role |
| --------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `From`, `To` | Narrow by participant (E.164). |
| `DateSentAfter`, `DateSentBefore` | Inclusive-ish window (provider maps to APIs). `DateSentBefore` doubles as the next-page cursor: copy `NextCursor` from the previous page into `DateSentBefore`. |
| `PageSize` | 1 to 1000 (default 50). |

`SmsMessageQueryResults<T>` exposes `Items`, `HasMore`, `NextCursor`, plus legacy `Start`, `Amount`, `Total` fields for callers that assumed offset pagination.

## `SmsServiceOptions` (abstract)

- `DefaultFromPhoneNumber`. Optional default `From`.
- `BulkSmsConcurrencyLimit`. Semaphore limit for concurrent bulk sends (default 10).
- `MaxMessageBodyLength` / `MaxBulkSmsLimit`. Caps before hitting upstream APIs.
- `EnableMetrics`. When true, `SmsServiceBase` prefers a non-null `IMetrics`.

## `PhoneNumber`

Static helpers aligned with `Lyo.Sms` builders. `Normalize` strips formatting. 10-digit US numbers get `+1`. `IsValid` / `Regex` / `ValidFormats` pair with `InvalidFormatException` when validation fails. Treat `Normalize` as best-effort for display and routing, not a substitute for libphonenumber if compliance requires it.

## `Direction`

Twilio-aligned string values (`StringValue`) for message direction enums (`inbound`, `outbound-api`, and others). Used where logs or webhooks classify traffic.

## Event argument records

These pair with `SmsServiceBase` events: `SmsSendingEventArgs`, `SmsSentEventArgs`, `SmsBulkSendingEventArgs`, `BulkSmsSentEventArgs`. Subscribers receive `SmsRequest` / `Result<SmsRequest>` / `BulkResult<SmsRequest>` snapshots suitable for auditing. Persist via [`Lyo.Sms.Postgres`](../Lyo.Sms.Postgres/README.md) or app code if durability matters.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)