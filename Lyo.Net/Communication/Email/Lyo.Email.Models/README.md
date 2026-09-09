# Lyo.Email.Models

Shared models, options, failure codes, and event payloads for the [`Lyo.Email`](../Lyo.Email/README.md) SMTP sender.

## Request and result types

- `EmailRequest`. Sender, recipients, subject, optional `TextBody` / `HtmlBody`, and optional attachment metadata carried as the `Result<EmailRequest>.Data` payload.
- `EmailAttachment`. File name and bytes, plus optional `ContentType` and `MetadataJson`. The logging schema does not persist attachment bytes.
- `EmailResult`. `Result<EmailRequest>` that adds `MessageId`, `SentDate`, and `SmtpResponse`. Built via `EmailResult.FromSuccess`, `EmailResult.FromException`, and `EmailResult.FromError`.

## Options

- `EmailServiceOptions`. Settings for `EmailService` (host, port, SSL, default sender, optional SMTP credentials, metrics toggle, `BulkEmailConcurrencyLimit` (default `10`), `MaxBulkEmailLimit` (default `1000`), `MaxAttachmentCountPerEmail` (default `20`)). Section name constant: `EmailServiceOptions.SectionName = "EmailServiceOptions"`.
- `EmailServiceOptionsValidator`. `IValidateOptions<EmailServiceOptions>` that demands `Host`, port range `1..65535`, `DefaultFromAddress`/`DefaultFromName`, and `MaxAttachmentCountPerEmail > 0`.

## Failure codes

`EmailErrorCodes` constants hung on failed `EmailResult` values:

| Constant | Value | Meaning |
| -------------------- | --------------------------- | ---------------------------------------- |
| `SendFailed` | `EMAIL_SEND_FAILED` | The SMTP send threw. |
| `BuildFailed` | `EMAIL_BUILD_FAILED` | The builder could not produce a message. |
| `OperationCancelled` | `EMAIL_OPERATION_CANCELLED` | The operation was cancelled. |

## Event payloads

- `EmailSendingEventArgs(EmailRequest EmailRequest)`. Raised before a single send.
- `EmailSentEventArgs(EmailResult EmailResult)`. After one send (success or failure). `EmailResult` carries `MessageId`, `SentDate`, and `SmtpResponse`.
- `EmailBulkSendingEventArgs(IReadOnlyList<EmailRequest> BulkEmailMessage)`. Before a bulk send starts.
- `BulkEmailSentEventArgs(BulkResult<EmailRequest> BulkEmailResult)`. After a bulk send finishes.
- `ConnectionTestedEventArgs(bool IsSuccess, TimeSpan ElapsedTime, Exception? Exception)`. After `TestConnectionAsync` finishes.

## Supported frameworks

`netstandard2.0;net10.0`

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Microsoft.Extensions.Options` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)