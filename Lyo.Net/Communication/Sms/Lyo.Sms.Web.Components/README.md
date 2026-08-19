# Lyo.Sms.Web.Components

**Blazor (MudBlazor)** workbench UI for exercising an injected **`ISmsService`** (provider-neutral `Result<SmsRequest>` surface). Depends on MudBlazor/snackbar primitives from [ `Lyo.Web.Components`](../../../Integration/Web/Lyo.Web.Components/README.md).

---

## Component: **`SmsWorkbench`**

Renders a small operator panel:

- **Recipients** — **`LyoChipInput`** constrained by **`RegexPatterns.PhoneNumberRegex`** (consistent with SMS stack validation UX).
- **From override** — optional **`LyoNullableTextField`** forwarded to **`BulkSmsBuilder.SetDefaultFrom`**.
- **Public media URLs** — chip list validated as **`http`/`https`**; each URL becomes an MMS attachment (see below).
- **Body** — multiline Mud text field.

Actions:

| Button | Behavior |
| ----------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Send SMS / MMS** | Builds **`BulkSmsBuilder`**, adds **one row per recipient** with shared body; for **each recipient**, **every** configured media chip is appended via **`AddAttachment`**, so duplicates appear if multiple recipients × same media intent—adjust if you fork the component. |
| **Test SMS Connection** | Calls **`SmsService.TestConnectionAsync()`**. |

Outcome panel shows **`BulkResult<SmsRequest>`** summary (total/success/failure/error strings) plus a per-row table using **`LyoResultErrorFormatter`** on failures.

**`ISnackbar`** mirrors status banners for rapid feedback.

---

## Component: **`TwilioSmsLogGrid`**

Projected grid over **`POST {BaseRoute}/TwilioSmsLogEntity/QueryProject`** (default base **`Twilio`**). **SID** / **Account SID** use compact **`LyoIdField`**. **Sent** / **Logged** (and hidden **Created**) use **`LyoTimestamp`** (nullable). **Received**, **Direction**, and **Status** are color chips. **Body** is truncated with ellipsis; hover shows the full text. Row menu **Open chat** queries **`To` or `From` = that person** (outbound → row **To**, inbound → row **From**) and pages until all matching log rows are loaded. Header **Refresh** re-runs that query. Composer sends via **`ISmsService.SendSmsAsync`**, then **`POST`**s the existing create route. Pass **`IApiClient`**; the host must also register **`ISmsService`**. ---

## Setup expectations

In the consuming host's **`Program.cs`** / startup: 1. Register a real **`ISmsService`** (e.g. **`AddTwilioSmsService*`**). 2. Add MudBlazor + [`Lyo.Web.Components`](../../../Integration/Web/Lyo.Web.Components/README.md) services the host already uses internally. 3. Render **`<SmsWorkbench />`** and/or **`<TwilioSmsLogGrid ApiClient="…" />`** inside an authenticated or internal-only area—these components have **no** rate limiting UI. Reminder from the Razor copy: MMS media must be **publicly reachable** by the SMS aggregator; unsupported providers quietly ignore attachments. ---

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Client` — (direct, lyo)
- `Lyo.Sms` — (direct, lyo)
- `Lyo.Web.Components` — (direct, lyo)
- `Lyo.Web.Components.Export` — (direct, lyo)
- `Lyo.Web.Components.Export.Csv` — (direct, lyo)
- `Lyo.Web.Components.Export.Xlsx` — (direct, lyo)
- `MudBlazor` `9.3` — (direct, third-party)
- `Lyo.Api.Models` — (transitive, lyo)
- `Lyo.Cache` — (transitive, lyo)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.Compression` — (transitive, lyo)
- `Lyo.DataTable.Models` — (transitive, lyo)
- `Lyo.DateAndTime` — (transitive, lyo)
- `Lyo.Diagnostic` — (transitive, lyo)
- `Lyo.Encryption` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Lyo.Hashing` — (transitive, lyo)
- `Lyo.Health` — (transitive, lyo)
- `Lyo.IO.Temp` — (transitive, lyo)
- `Lyo.KeyStore` — (transitive, lyo)
- `Lyo.Metrics` — (transitive, lyo)
- `Lyo.PackageMetadata` — (transitive, lyo)
- `Lyo.Query` — (transitive, lyo)
- `Lyo.Query.Models` — (transitive, lyo)
- `Lyo.Result` — (transitive, lyo)
- `Lyo.Sms.Models` — (transitive, lyo)
- `Lyo.Streams` — (transitive, lyo)
- `Lyo.Validation` — (transitive, lyo)
- `Blazored.LocalStorage` `4.5.0` — (transitive, third-party)
- `BouncyCastle.Cryptography` `2.6.2` — (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` — (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` — (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `System.Buffers` `4.6.1` — (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` — (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (transitive, microsoft, netstandard2.0)