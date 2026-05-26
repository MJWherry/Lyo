# Lyo.Email.Models

Shared models, options, error codes, and event arguments for the [`Lyo.Email`](../Lyo.Email/README.md)
SMTP service.

## What ships in this package

### Request / result records

- `EmailRequest` — sender, recipients, subject and optional attachment metadata used as the
  `Result<EmailRequest>.Data` payload.
- `EmailAttachment` — file name and bytes, plus optional `FileStorageId`, `TemplateId`, `ContentType`,
  and `MetadataJson` slots for audit and templating correlation. Attachment bytes are not persisted by
  the logging schema.
- `EmailResult` — `Result<EmailRequest>` specialisation that adds `MessageId`, `SentDate`, and
  `SmtpResponse`. Constructed via `EmailResult.FromSuccess`, `EmailResult.FromException`, and
  `EmailResult.FromError`.

### Options

- `EmailServiceOptions` — configuration for `EmailService` (host, port, SSL, default sender, optional
  SMTP credentials, metrics toggle, `BulkEmailConcurrencyLimit` (default `10`), `MaxBulkEmailLimit`
  (default `1000`), `MaxAttachmentCountPerEmail` (default `20`)). Section name constant:
  `EmailServiceOptions.SectionName = "EmailServiceOptions"`.
- `EmailServiceOptionsValidator` — `IValidateOptions<EmailServiceOptions>` enforcing required
  `Host`, port range `1..65535`, required `DefaultFromAddress`/`DefaultFromName`, and
  `MaxAttachmentCountPerEmail > 0`.

### Error codes

`EmailErrorCodes` constants attached to failed `EmailResult` values:

| Constant             | Value                       | Meaning                                |
|----------------------|-----------------------------|----------------------------------------|
| `SendFailed`         | `EMAIL_SEND_FAILED`         | The SMTP send raised an exception.     |
| `BuildFailed`        | `EMAIL_BUILD_FAILED`        | The builder failed to build a message. |
| `OperationCancelled` | `EMAIL_OPERATION_CANCELLED` | The operation was cancelled.           |

### Event arguments

Raised by `Lyo.Email.EmailService`:

- `EmailSendingEventArgs(EmailRequest EmailRequest)` — before a single send.
- `EmailSentEventArgs(Result<EmailRequest> EmailResult)` — after a single send (success or failure).
- `EmailBulkSendingEventArgs(IReadOnlyList<EmailRequest> BulkEmailMessage)` — before a bulk send begins.
- `BulkEmailSentEventArgs(BulkResult<EmailRequest> BulkEmailResult)` — after a bulk send completes.
- `ConnectionTestedEventArgs(bool IsSuccess, TimeSpan ElapsedTime, Exception? Exception)` — after
  `TestConnectionAsync` completes.

## Target frameworks

`netstandard2.0;net10.0`

## Related projects

- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
- [`Lyo.Result`](../../../Core/Result/Lyo.Result/README.md)
- [`Lyo.Email`](../Lyo.Email/README.md)
