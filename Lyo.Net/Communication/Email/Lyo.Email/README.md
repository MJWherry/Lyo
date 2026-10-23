# Lyo.Email

Provider-neutral email contracts, request builder, and send pipeline. `EmailRequest` is the message both providers send. `EmailServiceBase` applies the default From, attachment and bulk limits, metrics, logging, and events. Register `Lyo.Email.Smtp` or `Lyo.Email.SendGrid` to send. This package does not reference a mail SDK.

## Features

- **EmailRequest.** Sender, `EmailAddress` recipients, subject, text and HTML bodies, attachments, reply-to, headers, and priority.
- **EmailRequestBuilder.** Fluent helper. `Build()` returns an `EmailRequest`. An omitted display name is stored as the address.
- **EmailServiceBase.** Shared pipeline. Providers implement `SendCoreAsync` and `TestConnectionCoreAsync`.
- **From.** A builder From wins. The explicit from-address overload replaces it. `SendEmailAsync(EmailRequest)` does not write the default From onto the result when From was null.
- **Limits.** Over `MaxAttachmentCountPerEmail` or `MaxBulkEmailLimit` throws `ArgumentOutsideRangeException` before send. An empty bulk list returns an empty result and does not connect.
- **Cancellation.** A cancelled send returns a failed `EmailResult` with `EmailErrorCodes.OperationCancelled`. It does not throw. A cancelled connection probe throws `OperationCanceledException`.
- **Results.** Single sends return `EmailResult`, including `MessageId`, `SentDate`, and `ProviderResponse`.
- **Events.** `EmailSending`, `EmailSent`, `BulkSending`, `BulkEmailSent`, `ConnectionTested`.
- **Metrics.** Optional shared counters. `EnableMetrics == false` forces `NullMetrics`.
- **Attachments.** Bytes, streams, files, and ZIP helpers. `MetadataJson` stays on the request and is not sent.

## Examples

### Build a request

```csharp
var request = EmailRequestBuilder.New()
    .SetFrom("noreply@example.com", "My Application")
    .AddTo("ada@example.com", "Ada")
    .SetReplyTo("reply@example.com", "Replies")
    .AddHeader("X-Custom", "yes")
    .SetPriority(EmailPriority.Urgent)
    .SetSubject("Hello")
    .SetTextBody("Plain")
    .SetHtmlBody("<p>Hello</p>")
    .AddAttachment("note.txt", bytes, "text/plain")
    .Build();
```

### Send through a registered provider

```csharp
var result = await emailService.SendEmailAsync(builder, ct);
if (result.IsSuccess)
{
    Console.WriteLine(result.MessageId);
    Console.WriteLine(result.ProviderResponse);
}
```

### Shared options

```csharp
public sealed class ExampleEmailOptions : EmailServiceOptions
{
    public override void Validate()
    {
        base.Validate();
    }
}

var options = new ExampleEmailOptions
{
    DefaultFromAddress = "noreply@example.com",
    DefaultFromName = "My Application",
    EnableMetrics = false
};
```

## Which From address wins

## Providers

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)