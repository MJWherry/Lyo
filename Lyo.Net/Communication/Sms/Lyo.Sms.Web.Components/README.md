# Lyo.Sms.Web.Components

**Blazor (MudBlazor)** workbench UI for exercising an injected **`ISmsService`** (provider-neutral `Result<SmsRequest>` surface). Depends on MudBlazor/snackbar primitives from [
`Lyo.Web.Components`](../../../Integration/Web/Lyo.Web.Components/README.md).

---

## Component: **`SmsWorkbench`**

Renders a small operator panel:

- **Recipients** — **`LyoChipInput`** constrained by **`RegexPatterns.PhoneNumberRegex`** (consistent with SMS stack validation UX).
- **From override** — optional **`LyoNullableTextField`** forwarded to **`BulkSmsBuilder.SetDefaultFrom`**.
- **Public media URLs** — chip list validated as **`http`/`https`**; each URL becomes an MMS attachment (see below).
- **Body** — multiline Mud text field.

Actions:

| Button                  | Behavior                                                                                                                                                                                                                                                                     |
|-------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Send SMS / MMS**      | Builds **`BulkSmsBuilder`**, adds **one row per recipient** with shared body; for **each recipient**, **every** configured media chip is appended via **`AddAttachment`**, so duplicates appear if multiple recipients × same media intent—adjust if you fork the component. |
| **Test SMS Connection** | Calls **`SmsService.TestConnectionAsync()`**.                                                                                                                                                                                                                                |

Outcome panel shows **`BulkResult<SmsRequest>`** summary (total/success/failure/error strings) plus a per-row table using **`LyoResultErrorFormatter`** on failures.

**`ISnackbar`** mirrors status banners for rapid feedback.

---

## Setup expectations

In **`Program.cs`** / host startup:

1. Register a real **`ISmsService`** (e.g. **`AddTwilioSmsService*`**).
2. Add MudBlazor + [`Lyo.Web.Components`](../../../Integration/Web/Lyo.Web.Components/README.md) services the host already uses internally.
3. Render **`<SmsWorkbench />`** inside an authenticated or internal-only area—this component has **no** rate limiting UI.

Reminder from the Razor copy: MMS media must be **publicly reachable** by the SMS aggregator; unsupported providers quietly ignore attachments.

---

## Related projects

- [`Lyo.Sms`](../Lyo.Sms/README.md)
- [`Lyo.Web.Components`](../../../Integration/Web/Lyo.Web.Components/README.md)
