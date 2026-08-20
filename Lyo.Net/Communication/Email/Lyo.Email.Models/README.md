# Lyo.Email.Models

Shared models, options, error codes, and event arguments for the [`Lyo.Email`](../Lyo.Email/README.md) SMTP service.

## Request and result records

- `EmailRequest`. Sender, recipients, subject, and optional attachment metadata used as the `Result<EmailRequest>.Data` payload.
- `EmailAttachment`. File name and bytes, plus optional `FileStorageId`, `TemplateId`, `ContentType`, and `MetadataJson` slots for audit and templating correlation. Attachment bytes are not persisted by the logging schema.
- `EmailResult`. `Result<EmailRequest>` that adds `MessageId`, `SentDate`, and `SmtpResponse`. Constructed via `EmailResult.FromSuccess`, `EmailResult.FromException`, and `EmailResult.FromError`.

## Options

- `EmailServiceOptions`. Configuration for `EmailService` (host, port, SSL, default sender, optional SMTP credentials, metrics toggle, `BulkEmailConcurrencyLimit` (default `10`), `MaxBulkEmailLimit` (default `1000`), `MaxAttachmentCountPerEmail` (default `20`)). Section name constant: `EmailServiceOptions.SectionName = "EmailServiceOptions"`.
- `EmailServiceOptionsValidator`. `IValidateOptions<EmailServiceOptions>` that requires `Host`, port range `1..65535`, `DefaultFromAddress`/`DefaultFromName`, and `MaxAttachmentCountPerEmail > 0`.

## Error codes

`EmailErrorCodes` constants attached to failed `EmailResult` values:

| Constant | Value | Meaning |
| -------------------- | --------------------------- | -------------------------------------- |
| `SendFailed` | `EMAIL_SEND_FAILED` | The SMTP send raised an exception. |
| `BuildFailed` | `EMAIL_BUILD_FAILED` | The builder failed to build a message. |
| `OperationCancelled` | `EMAIL_OPERATION_CANCELLED` | The operation was cancelled. |

## Event arguments

- `EmailSendingEventArgs(EmailRequest EmailRequest)`. Before a single send.
- `EmailSentEventArgs(Result<EmailRequest> EmailResult)`. After a single send (success or failure).
- `EmailBulkSendingEventArgs(IReadOnlyList<EmailRequest> BulkEmailMessage)`. Before a bulk send begins.
- `BulkEmailSentEventArgs(BulkResult<EmailRequest> BulkEmailResult)`. After a bulk send completes.
- `ConnectionTestedEventArgs(bool IsSuccess, TimeSpan ElapsedTime, Exception? Exception)`. After `TestConnectionAsync` completes.

## Target frameworks

`netstandard2.0;net10.0`

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Microsoft.Extensions.Options` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)