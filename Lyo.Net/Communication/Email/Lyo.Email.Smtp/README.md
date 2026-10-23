# Lyo.Email.Smtp

MailKit SMTP implementation of `Lyo.Email`. `SmtpEmailService` maps one `EmailRequest` to a MIME message, including display names, content types, reply-to, headers, and priority. One connection per single send. A bulk send reuses one connection. `AddSmtpEmailServiceFromConfiguration` binds the `SmtpEmailOptions` section.

## Features

- **SMTP.** Delivery goes through MailKit.
- **MIME.** Recipient names, attachment content types, reply-to, custom headers, and priority are written onto the message.
- **Bulk.** One SMTP connection for the batch. `BulkEmailConcurrencyLimit` does not change that path.
- **Auth.** Authentication runs only when both username and password are set.
- **Probe.** `TestConnectionAsync` connects and disconnects. Cancellation throws. Any other failure returns false.
- **Parser.** `EmailMimeParser` reads a `.eml` payload. It does not talk to a mailbox.
- **DI.** `AddSmtpEmailService`, the options instance overload, and `AddSmtpEmailServiceFromConfiguration`.
- **Secrets.** `ToString()` includes the username and omits the password.

## Examples

### Set SMTP options

```json
{
  "SmtpEmailOptions": {
    "Host": "smtp.example.com",
    "Port": 587,
    "UseSsl": false,
    "DefaultFromAddress": "noreply@example.com",
    "DefaultFromName": "My Application",
    "Username": "smtp_username",
    "Password": "smtp_password",
    "EnableMetrics": false
  }
}
```

### Register from config

```csharp
services.AddSmtpEmailServiceFromConfiguration(configuration);
// services.AddSmtpEmailServiceFromConfiguration(configuration, "MySection");
```

### Register via a callback

```csharp
services.AddSmtpEmailService(options =>
{
    options.Host = "smtp.example.com";
    options.Port = 587;
    options.UseSsl = false;
    options.DefaultFromAddress = "noreply@example.com";
    options.DefaultFromName = "My Application";
});
```

## Connection security

## Bulk failures

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Email` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `MailKit` `4.17.0` (direct, third-party)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)