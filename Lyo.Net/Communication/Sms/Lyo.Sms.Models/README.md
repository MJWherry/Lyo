# Lyo.Sms.Models

Shared **domain types** for [`Lyo.Sms`](../Lyo.Sms/README.md): payloads, paging, events, normalization, and base options. There is **no** SMS sending here—implementations live in
provider packages (`Lyo.Sms.Twilio`, etc.).

---

## **`SmsRequest`**

Canonical wire shape for outbound SMS/MMS:

- **`To`** / **`From`** — E.164 preferred; builders and services normalize many US-centric inputs.
- **`Body`** — text; combined length validated against **`SmsServiceOptions.MaxMessageBodyLength`** in the core library.
- **`MediaUrls`** — `List<Uri>` for MMS attachments (empty for plain SMS).

`ToString()` truncates bodies for **`DebuggerDisplay`**-friendly diagnostics.

---

## **`SmsMessageQueryFilter`** / **`SmsMessageQueryResults<T>`**

**Cursor-based listing** used by **`ISmsService.GetMessagesAsync`**:

| Field                                     | Role                                                                                                                                                                        |
|-------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **`From`**, **`To`**                      | Narrow by participant (E.164).                                                                                                                                              |
| **`DateSentAfter`**, **`DateSentBefore`** | Inclusive-ish window (provider maps to APIs). **`DateSentBefore`** doubles as **next-page cursor**: copy **`NextCursor`** from the previous page into **`DateSentBefore`**. |
| **`PageSize`**                            | 1–1000 (default 50).                                                                                                                                                        |

`SmsMessageQueryResults<T>` exposes **`Items`**, **`HasMore`**, **`NextCursor`**, plus legacy **`Start`**, **`Amount`**, **`Total`** fields for callers that assumed offset
pagination.

---

## **`SmsServiceOptions`** (abstract)

Base knobs every provider inherits:

- **`DefaultFromPhoneNumber`** — optional default **`From`**.
- **`BulkSmsConcurrencyLimit`** — semaphore limit for concurrent bulk sends (default 10).
- **`MaxMessageBodyLength`** / **`MaxBulkSmsLimit`** — guardrails before hitting upstream APIs.
- **`EnableMetrics`** — when true, **`SmsServiceBase`** prefers a non-null **`IMetrics`**.

Concrete options (Twilio credentials, etc.) subclass this in provider assemblies.

---

## **`PhoneNumber`**

Static helpers aligned with **`Lyo.Sms` builders**:

- **`Normalize`** strips formatting; 10-digit US numbers get **`+1`**; aligns with permissive-but-predictable behavior in the stack.
- **`IsValid`** / **`Regex`** / **`ValidFormats`** — paired with **`InvalidFormatException`** when validation fails aggressively.

Treat **`Normalize`** as "best effort" for display and routing—not a substitute for full libphonenumber validation if compliance requires it.

---

## **`Direction`**

Twilio-aligned string values (**`StringValue`**) for message direction enums (`inbound`, `outbound-api`, …). Used where logs or webhooks classify traffic.

---

## **Event argument records**

These pair with **`SmsServiceBase`** events:

- **`SmsSendingEventArgs`**, **`SmsSentEventArgs`**
- **`SmsBulkSendingEventArgs`**, **`BulkSmsSentEventArgs`**

Subscribers receive **`SmsRequest`** / `Result<SmsRequest>` / `BulkResult<SmsRequest>` snapshots suitable for auditing (but **persist** via [
`Lyo.Sms.Postgres`](../Lyo.Sms.Postgres/README.md) or app code if durability matters).

---

## Related projects

- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md) — guards, helpers
- [`Lyo.Result`](../../../Core/Result/Lyo.Result/README.md) — `Result<T>`, `BulkResult<T>`
- [`Lyo.Sms`](../Lyo.Sms/README.md) — **`ISmsService`**, **`SmsMessageBuilder`**, **`BulkSmsBuilder`**
