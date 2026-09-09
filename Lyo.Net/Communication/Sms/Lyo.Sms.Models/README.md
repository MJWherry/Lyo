# Lyo.Sms.Models

Shared types used by [`Lyo.Sms`](../Lyo.Sms/README.md): payloads, paging, events, normalization, and base options. The package never sends SMS. Implementations live in provider packages (`Lyo.Sms.Twilio`, and others).

## `SmsRequest`

Canonical form for outbound SMS/MMS. `To` / `From` prefer E.164. Builders and services normalize many US-centric inputs. `Body` is text. Combined length is validated against `SmsServiceOptions.MaxMessageBodyLength` in the core library. `MediaUrls` is a `List<Uri>` for MMS attachments (empty for plain SMS). `ToString()` truncates bodies for `DebuggerDisplay`.

## `SmsMessageQueryFilter` / `SmsMessageQueryResults<T>`

Cursor listing used by `ISmsService.GetMessagesAsync`:

| Field | Role |
| --------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `From`, `To` | Filter by participant (E.164). |
| `DateSentAfter`, `DateSentBefore` | Inclusive-ish window (the provider maps it to APIs). `DateSentBefore` also acts as the next-page cursor: copy `NextCursor` from the previous page into `DateSentBefore`. |
| `PageSize` | 1 to 1000 (default 50). |

`SmsMessageQueryResults<T>` exposes `Items`, `HasMore`, `NextCursor`, plus legacy `Start`, `Amount`, `Total` fields for callers that assumed offset paging.

## `SmsServiceOptions` (abstract)

- `DefaultFromPhoneNumber`. Optional default `From`.
- `BulkSmsConcurrencyLimit`. Semaphore cap for concurrent bulk sends (default 10).
- `MaxMessageBodyLength` / `MaxBulkSmsLimit`. Limits applied before calling upstream APIs.
- `EnableMetrics`. When true, `SmsServiceBase` uses a non-null `IMetrics` when one is available.

## `PhoneNumber`

Static helpers that match `Lyo.Sms` builders. `Normalize` strips formatting. 10-digit US numbers get `+1`. `IsValid` / `Regex` / `ValidFormats` pair with `InvalidFormatException` when validation fails. Treat `Normalize` as best-effort for display and routing, not a substitute for libphonenumber if compliance requires it.

## `Direction`

Twilio-aligned string values (`StringValue`) for message direction enums (`inbound`, `outbound-api`, and others). Used wherever logs or webhooks classify traffic.

## Event payload records

These go with `SmsServiceBase` events: `SmsSendingEventArgs`, `SmsSentEventArgs`, `SmsBulkSendingEventArgs`, `BulkSmsSentEventArgs`. Subscribers get `SmsRequest` / `Result<SmsRequest>` / `BulkResult<SmsRequest>` snapshots suitable for auditing. Persist through [`Lyo.Sms.Postgres`](../Lyo.Sms.Postgres/README.md) or app code when durability matters.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)