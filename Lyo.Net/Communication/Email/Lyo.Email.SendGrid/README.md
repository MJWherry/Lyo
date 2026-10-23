# Lyo.Email.SendGrid

Twilio SendGrid implementation of `Lyo.Email`. One `EmailRequest` becomes one `SendGridMessage` with one personalization holding every To, Cc, and Bcc. `AddSendGridEmailServiceFromConfiguration` binds the `SendGridEmailOptions` section. A 2xx response is success. Non-2xx, including 429, is a failed `EmailResult` with no retry.

## Features

- **SendGrid.** One API call per message through `ISendGridClient`.
- **Recipients.** Display names are sent. Recipients on one personalization can see each other.
- **Attachments.** Base64, with filename, content type, and disposition `attachment`. `MetadataJson` is not sent.
- **Reply-to.** One address uses `reply_to`. Several use `reply_to_list` only.
- **Tracking.** Click, open, and subscription tracking are turned off on each message.
- **Priority.** `NonUrgent` and `Urgent` set the `Priority` header to `non-urgent` and `urgent`. `Normal` leaves that header unset, matching MailKit.
- **Message id.** `X-Message-Id` may be missing on a 202. The result is still success.
- **Probe.** `TestConnectionAsync` calls `GET scopes`.
- **Bulk.** The base fans out one API call per message, bounded by `BulkEmailConcurrencyLimit`.
- **DI.** `AddSendGridEmailService`, the options instance overload, and `AddSendGridEmailServiceFromConfiguration`. One singleton `SendGridClient` is shared.
- **Secrets.** `ToString()` omits the API key.

## Examples

### Set SendGrid options

```json
{
  "SendGridEmailOptions": {
    "ApiKey": "your_api_key",
    "DefaultFromAddress": "noreply@example.com",
    "DefaultFromName": "My Application",
    "EnableMetrics": false
  }
}
```

### Register from config

```csharp
services.AddSendGridEmailServiceFromConfiguration(configuration);
```

### Register via a callback

```csharp
services.AddSendGridEmailService(options =>
{
    options.ApiKey = "your_api_key";
    options.DefaultFromAddress = "noreply@example.com";
    options.DefaultFromName = "My Application";
});
```

## HTTP results

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Email` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (direct, microsoft)
- `SendGrid` `9.29.3` (direct, third-party)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)