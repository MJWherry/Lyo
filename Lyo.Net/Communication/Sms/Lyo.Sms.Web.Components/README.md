# Lyo.Sms.Web.Components

Blazor (MudBlazor) workbench for an injected `ISmsService`. Uses MudBlazor and snackbar helpers from [`Lyo.Web.Components`](../../../Integration/Web/Lyo.Web.Components/README.md).

## `SmsWorkbench`

Renders a small operator panel:

- **Recipients.** `LyoChipInput` constrained by `RegexPatterns.PhoneNumberRegex` (same validation as the SMS stack).
- **From override.** Optional `LyoNullableTextField` forwarded to `BulkSmsBuilder.SetDefaultFrom`.
- **Public media URLs.** Chip list validated as `http`/`https`. Each URL becomes an MMS attachment (see below).
- **Body.** Multiline Mud text field.

Actions:

| Button | Behavior |
| ------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Send SMS / MMS | Builds `BulkSmsBuilder` and adds one row per recipient with a shared body. For each recipient, every configured media chip is appended via `AddAttachment`, so the same media URL is attached once per recipient. Adjust that if you fork the component. |
| Test SMS Connection | Calls `SmsService.TestConnectionAsync()`. |

The outcome panel shows a `BulkResult<SmsRequest>` summary (total/success/failure/error strings) plus a per-row table using `LyoResultErrorFormatter` on failures.

`ISnackbar` mirrors status banners.

## `TwilioSmsLogGrid`

Projected grid over `POST {BaseRoute}/TwilioSmsLogEntity/QueryProject` (default base `Twilio`). SID and Account SID use compact `LyoIdField`. Sent, Logged, and the hidden Created column use `LyoTimestamp` (nullable). Received, Direction, and Status are color chips. Body is truncated with ellipsis. Hover shows the full text. Quick search ORs SID (`Id`), From, To, and Body. Row menu Open chat queries `To` or `From` = that person (outbound uses row To, inbound uses row From) and pages until all matching log rows are loaded. Header Refresh re-runs that query. Composer sends via `ISmsService.SendSmsAsync`, then POSTs the existing create route. Pass `IApiClient`. The host must also register `ISmsService`.

## Host setup

In the consuming host's `Program.cs` / startup: 1. Register a real `ISmsService` (for example `AddTwilioSmsService*`). 2. Add MudBlazor plus [`Lyo.Web.Components`](../../../Integration/Web/Lyo.Web.Components/README.md) services the host already uses. 3. Render `<SmsWorkbench />` and/or `<TwilioSmsLogGrid ApiClient="…" />` inside an authenticated or internal-only area. These components have no rate limiting UI. MMS media must be publicly reachable by the SMS aggregator. Unsupported providers ignore attachments.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Client` (direct, lyo)
- `Lyo.Sms` (direct, lyo)
- `Lyo.Web.Components` (direct, lyo)
- `Lyo.Web.Components.Export` (direct, lyo)
- `Lyo.Web.Components.Export.Csv` (direct, lyo)
- `Lyo.Web.Components.Export.Xlsx` (direct, lyo)
- `MudBlazor` `9.3` (direct, third-party)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Cache` (transitive, lyo)
- `Lyo.Common` (transitive, lyo)
- `Lyo.Compression` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.IO.Temp` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Query` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Sms.Models` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Blazored.LocalStorage` `4.5.0` (transitive, third-party)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)