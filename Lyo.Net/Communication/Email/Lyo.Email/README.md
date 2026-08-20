# Lyo.Email

SMTP email through MailKit. `EmailService` implements `IEmailService`.

## Features

- **EmailRequestBuilder.** Fluent builder for constructing emails.
- **SMTP.** Sends through MailKit.
- **Bulk.** Sequential bulk send over a single SMTP connection with per-message results.
- **Attachments.** File attachments and `ZipFileBuilder` for bundling files into one ZIP.
- **HTML and text.** HTML and plain-text bodies.
- **Results.** Failures return `Result<EmailRequest>` (`EmailResult` for single sends).
- **Logging.** Logs through Microsoft.Extensions.Logging.
- **Metrics.** Optional metrics on email operations.
- **DI.** `AddEmailService` and `AddEmailServiceFromConfiguration`.
- **Async.** Methods take `CancellationToken`.
- **Events.** `EmailSending`, `EmailSent`, `BulkSending`, `BulkEmailSent`, `ConnectionTested`.
- **Validation.** `EmailServiceOptionsValidator` checks required options.

## Examples

### Configure email options

```json
{
  "EmailServiceOptions": {
    "Host": "smtp.example.com",
    "Port": 587,
    "UseSsl": true,
    "DefaultFromAddress": "noreply@example.com",
    "DefaultFromName": "My Application",
    "Username": "smtp_username",
    "Password": "smtp_password",
    "EnableMetrics": false
  }
}
```

### Configure email options (2)

```csharp
var options = new EmailServiceOptions
{
    Host = "smtp.example.com",
    Port = 587,
    UseSsl = true,
    DefaultFromAddress = "noreply@example.com",
    DefaultFromName = "My Application",
    Username = "smtp_username",
    Password = "smtp_password",
    EnableMetrics = false
};
```

### Register from configuration

```csharp
// In ConfigureServices(context, services):
services.AddEmailServiceFromConfiguration(context.Configuration);
// Override the configuration section name if needed (defaults to "EmailServiceOptions"):
// services.AddEmailServiceFromConfiguration(context.Configuration, "MySection");
```

### Register with an action

```csharp
services.AddEmailService(options =>
{
    options.Host = "smtp.example.com";
    options.Port = 587;
    options.UseSsl = true;
    options.DefaultFromAddress = "noreply@example.com";
    options.DefaultFromName = "My Application";
    options.Username = "smtp_username";
    options.Password = "smtp_password";
});
```

### Register from IServiceProvider

```csharp
services.AddEmailService(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    return new EmailServiceOptions
    {
        Host = config["Smtp:Host"],
        Port = int.Parse(config["Smtp:Port"] ?? "587"),
        UseSsl = bool.Parse(config["Smtp:UseSsl"] ?? "true"),
        DefaultFromAddress = config["Smtp:DefaultFromAddress"]!,
        DefaultFromName = config["Smtp:DefaultFromName"]!,
        Username = config["Smtp:Username"],
        Password = config["Smtp:Password"]
    };
});
```

### Register with a minimal action

```csharp
services.AddEmailService(options => {
    options.Host = "smtp.example.com";
    options.Port = 587;
    options.DefaultFromAddress = "noreply@example.com";
    options.DefaultFromName = "My Application";
});
```

### Send a welcome email

```csharp
public class MyService
{
    private readonly IEmailService _emailService;
    
    public MyService(IEmailService emailService)
    {
        _emailService = emailService;
    }
    
    public async Task SendWelcomeEmailAsync(string recipientEmail)
    {
        var builder = EmailRequestBuilder.New()
            .SetSubject("Welcome!")
            .SetHtmlBody("<h1>Welcome to our service!</h1><p>Thank you for joining.</p>")
            .SetTextBody("Welcome to our service! Thank you for joining.")
            .AddTo(recipientEmail, "New User");
        
        var result = await _emailService.SendEmailAsync(builder);
        
        if (result.IsSuccess)
        {
            Console.WriteLine($"Email sent! Message ID: {(result as EmailResult)?.MessageId}");
        }
        else
        {
            Console.WriteLine($"Failed: {result.Errors?.FirstOrDefault()?.Message}");
        }
    }
}
```

### Basic email

```csharp
var builder = EmailRequestBuilder.New()
    .SetSubject("Hello")
    .SetTextBody("This is a test email")
    .AddTo("recipient@example.com", "Recipient Name");

var result = await _emailService.SendEmailAsync(builder);
```

### HTML email

```csharp
var builder = EmailRequestBuilder.New()
    .SetSubject("HTML Email")
    .SetHtmlBody("<h1>Hello</h1><p>This is an <strong>HTML</strong> email.</p>")
    .SetTextBody("Hello. This is an HTML email.") // Plain text fallback
    .AddTo("recipient@example.com");

var result = await _emailService.SendEmailAsync(builder);
```

### Email with attachments

```csharp
var builder = EmailRequestBuilder.New()
    .SetSubject("Email with Attachment")
    .SetTextBody("Please find the attachment.")
    .AddTo("recipient@example.com")
    .AddAttachment("document.pdf", File.ReadAllBytes("path/to/document.pdf"));

var result = await _emailService.SendEmailAsync(builder);
```

### Multiple attachments as ZIP

```csharp
var zipBytes = ZipFileBuilder.New()
    .AddFile("file1.txt", Encoding.UTF8.GetBytes("Content 1"))
    .AddFile("file2.txt", "Content 2") // text overload (UTF-8 by default)
    .AddFileFromPath("/path/to/report.pdf") // from disk; entry name defaults to file name
    .AddDirectory("/path/to/docs", "docs/") // recurse a directory under a prefix
    .Build();

var builder = EmailRequestBuilder.New()
    .SetSubject("Files Attached")
    .SetTextBody("Please find the attached files.")
    .AddTo("recipient@example.com")
    .AddAttachment("files.zip", zipBytes);

var result = await _emailService.SendEmailAsync(builder);
```

### Custom from address

```csharp
var builder = EmailRequestBuilder.New()
    .SetSubject("From Custom Address")
    .SetTextBody("This email is from a custom address.")
    .SetFrom("custom@example.com", "Custom Sender")
    .AddTo("recipient@example.com");

// Use the builder's From address
var result = await _emailService.SendEmailAsync(builder);

// Or override it
var result2 = await _emailService.SendEmailAsync(builder, "override@example.com", "Override Name");
```

### Bulk email

```csharp
var builders = new[]
{
    EmailRequestBuilder.New()
        .SetSubject("Bulk Email 1")
        .SetTextBody("First email")
        .AddTo("user1@example.com"),
    EmailRequestBuilder.New()
        .SetSubject("Bulk Email 2")
        .SetTextBody("Second email")
        .AddTo("user2@example.com")
};

var results = await _emailService.SendBulkEmailAsync(builders);

foreach (var result in results)
{
    if (result.IsSuccess)
    {
        Console.WriteLine($"Sent to {result.Data?.ToAddresses?.FirstOrDefault()}: {(result as EmailResult)?.MessageId}");
    }
    else
    {
        Console.WriteLine($"Failed: {result.Errors?.FirstOrDefault()?.Message}");
    }
}
```

### Test connection

```csharp
var isConnected = await _emailService.TestConnectionAsync();
if (isConnected)
{
    Console.WriteLine("SMTP connection successful!");
}
```

### Subscribe to events

```csharp
_emailService.EmailSent += (sender, args) =>
{
    var result = args.EmailResult;
    if (result.IsSuccess)
    {
        Console.WriteLine($"Email sent successfully: {result.Data?.Subject}");
    }
    else
    {
        Console.WriteLine($"Email failed: {result.Errors?.FirstOrDefault()?.Message}");
    }
};

_emailService.BulkEmailSent += (sender, args) =>
{
    var bulkResult = args.BulkEmailResult;
    Console.WriteLine($"Bulk send completed: {bulkResult.SuccessCount}/{bulkResult.TotalCount} successful");
};

_emailService.ConnectionTested += (sender, args) =>
{
    if (args.IsSuccess)
    {
        Console.WriteLine($"Connection test passed in {args.ElapsedTime}");
    }
    else
    {
        Console.WriteLine($"Connection test failed: {args.Exception?.Message}");
    }
};
```

### EmailServiceOptions

```csharp
public class EmailServiceOptions
{
    /// <summary>SMTP server hostname. Required.</summary>
    public string Host { get; set; } = null!;
    
    /// <summary>SMTP server port. Default: 587.</summary>
    public int Port { get; set; } = 587;
    
    /// <summary>Whether to use SSL/TLS. Default: false.</summary>
    public bool UseSsl { get; set; } = false;
    
    /// <summary>Default from email address. Required.</summary>
    public string DefaultFromAddress { get; set; } = null!;
    
    /// <summary>Default from display name. Required.</summary>
    public string DefaultFromName { get; set; } = null!;
    
    /// <summary>SMTP username for authentication. Optional.</summary>
    public string? Username { get; set; }
    
    /// <summary>SMTP password for authentication. Optional.</summary>
    public string? Password { get; set; }
    
    /// <summary>Enable metrics collection. Default: false.</summary>
    public bool EnableMetrics { get; set; } = false;

    /// <summary>Soft cap used by single-call bulk concurrency planning. Default: 10.</summary>
    public int BulkEmailConcurrencyLimit { get; set; } = 10;

    /// <summary>Maximum number of messages allowed per <c>SendBulkEmailAsync</c> call. Default: 1000.</summary>
    public int MaxBulkEmailLimit { get; set; } = 1000;

    /// <summary>Maximum number of attachments allowed per email. Default: 20.</summary>
    public int MaxAttachmentCountPerEmail { get; set; } = 20;
}
```

### EmailSending

```csharp
_emailService.EmailSending += (sender, args) =>
{
    var request = args.EmailRequest;
    Console.WriteLine($"Sending email to {string.Join(", ", request.ToAddresses ?? [])}: {request.Subject}");
};
```

### EmailSent

```csharp
_emailService.EmailSent += (sender, args) =>
{
    var result = args.EmailResult;
    if (result.IsSuccess)
    {
        Console.WriteLine($"Email sent successfully: {(result as EmailResult)?.MessageId}");
    }
    else
    {
        Console.WriteLine($"Email failed: {result.Errors?.FirstOrDefault()?.Message}");
    }
};
```

### BulkSending

```csharp
_emailService.BulkSending += (sender, args) =>
{
    Console.WriteLine($"Starting bulk send for {args.BulkEmailMessage.Count} emails");
};
```

### BulkEmailSent

```csharp
_emailService.BulkEmailSent += (sender, args) =>
{
    var bulkResult = args.BulkEmailResult;
    Console.WriteLine($"Bulk send completed:");
    Console.WriteLine($" Total: {bulkResult.TotalCount}");
    Console.WriteLine($" Success: {bulkResult.SuccessCount}");
    Console.WriteLine($" Failure: {bulkResult.FailureCount}");
};
```

### ConnectionTested

```csharp
_emailService.ConnectionTested += (sender, args) =>
{
    if (args.IsSuccess)
    {
        Console.WriteLine($"Connection test successful in {args.ElapsedTime}");
    }
    else
    {
        Console.WriteLine($"Connection test failed: {args.Exception?.Message}");
    }
};
```

### Testing

```bash
dotnet test
```

## Configure email options

#### Using configuration file (appsettings.json)

#### Using code

## Register services

#### Using configuration binding

#### Using action

#### Using service provider

#### Using action (minimal)

## Multiple attachments as ZIP

`ZipFileBuilder` packages multiple files into a single ZIP byte array that can be attached like any other file. `ZipFileBuilder` is a one-shot builder. After calling `Build()`/`BuildToFile()`/`BuildToStream()` the archive is closed and the instance cannot be reused.

## Resilience

The library does not include built-in retry or timeout logic. Apply resilience at the application layer (for example using [Lyo.Resilience](https://www.nuget.org/packages/Lyo.Resilience)
or Polly) by wrapping calls to `IEmailService`:

```csharp
// Example: wrap email sends with IResilientExecutor
await _resilientExecutor.ExecuteAsync("email-pipeline", ct => _emailService.SendEmailAsync(builder, ct), cancellationToken);
```

## EmailServiceOptions

The configuration section name defaults to `EmailServiceOptions` (exposed as `EmailServiceOptions.SectionName`).

## Validation

- `Host` must not be null or empty
- `Port` must be between 1 and 65535
- `DefaultFromAddress` must not be null or empty
- `DefaultFromName` must not be null or empty
- `MaxAttachmentCountPerEmail` must be greater than 0

## Error handling

All email operations return `Result<EmailRequest>` (runtime type `EmailResult` for single sends):

```csharp
var result = await _emailService.SendEmailAsync(builder);

if (result.IsSuccess)
{
    Console.WriteLine($"Success: {result.Data?.Subject}");
    if (result is EmailResult er)
    {
        Console.WriteLine($"Message ID: {er.MessageId}");
        Console.WriteLine($"Sent Date: {er.SentDate}");
        Console.WriteLine($"SMTP Response: {er.SmtpResponse}");
    }
}
else
{
    var firstError = result.Errors?.FirstOrDefault();
    Console.WriteLine($"Error: {firstError?.Message}");
    if (firstError?.Exception != null)
    {
        Console.WriteLine($"Exception: {firstError.Exception.Message}");
    }
}
```

## `EmailResult` properties

- `IsSuccess`. Whether the operation succeeded.
- `Data`. The EmailRequest (recipients, subject, and related fields).
- `Errors`. List of errors if failed.
- `MessageId`. SMTP message ID (on EmailResult, when success).
- `SentDate`. When the email was sent (on EmailResult, when success).
- `SmtpResponse`. SMTP server response (on EmailResult, when success).

## Events

EmailService raises these events:

## EmailSending

Fired before each email is sent (including during bulk operations):

## EmailSent

Fired after each email is sent (success or failure):

## BulkSending

Fired before a bulk email operation starts:

## BulkEmailSent

Fired after a bulk email operation completes:

## ConnectionTested

Fired when a connection test completes:

## Logging

The library uses Microsoft.Extensions.Logging:

```csharp
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});
```

Log levels:

- **Information.** Successful operations, email details
- **Debug.** SMTP connection details, authentication
- **Warning.** Cancellations, disconnection errors
- **Error.** Failures, exceptions

## Metrics

- `email.send.duration`. Duration timer for send operations.
- `email.send.success`. Counter for successful sends.
- `email.send.failure`. Counter for failed sends.
- `email.send.cancelled`. Counter for cancelled sends.
- `email.send.last_duration_ms`. Gauge for last send duration.
- `email.bulk.send.duration`. Duration timer for bulk operations.
- `email.bulk.send.total`. Counter for total bulk emails.
- `email.bulk.send.success`. Counter for successful bulk emails.
- `email.bulk.send.failure`. Counter for failed bulk emails.
- `email.bulk.send.last_duration_ms`. Gauge for last bulk duration.
- `email.smtp.connect.duration`. SMTP connection duration.
- `email.smtp.authenticate.duration`. SMTP authentication duration.
- `email.test_connection.duration`. Connection test duration.
- `email.test_connection.success`. Counter for successful connection tests.
- `email.test_connection.failure`. Counter for failed connection tests.

## `IEmailService`

- `Task<Result<EmailRequest>> SendEmailAsync(EmailRequestBuilder requestBuilder, string fromAddress, string? fromName = null, CancellationToken ct = default)`. Send email with a custom from address.
- `Task<Result<EmailRequest>> SendEmailAsync(EmailRequestBuilder requestBuilder, CancellationToken ct = default)`. Send email with the default from address.
- `Task<Result<EmailRequest>> SendEmailAsync(EmailRequest request, CancellationToken ct = default)`. Send email using an EmailRequest object.
- `Task<IReadOnlyList<Result<EmailRequest>>> SendBulkEmailAsync(IEnumerable<EmailRequestBuilder> builders, CancellationToken ct = default)`. Send multiple emails sequentially.
- `Task<BulkResult<EmailRequest>> SendBulkEmailAsync(BulkEmailRequestBuilder bulkRequestBuilder, CancellationToken ct = default)`. Send bulk emails using BulkEmailRequestBuilder.
- `Task<bool> TestConnectionAsync(CancellationToken ct = default)`. Test SMTP connection.

## `EmailRequestBuilder`

- `AddTo(...)`. Add To recipients.
- `AddCc(...)`. Add Cc recipients.
- `AddBcc(...)`. Add Bcc recipients.
- `SetFrom(...)`. Set From address.
- `SetReplyTo(...)`. Set Reply-To address.
- `SetSubject(...)`. Set email subject.
- `SetPriority(...)`. Set message priority.
- `SetHtmlBody(...)`. Set HTML body.
- `SetTextBody(...)`. Set plain text body.
- `AppendHtmlBody(...)`. Append to HTML body.
- `AppendTextBody(...)`. Append to text body.
- `AddAttachment(...)`. Add file attachments (use `ZipFileBuilder` first if you want a ZIP attachment).
- `AddHeader(...)`. Add custom headers.
- `ClearTo()` / `ClearCc()` / `ClearBcc()` / `ClearAttachments()`. Clear collections.
- `Build()`. Build the MimeMessage.

## `ZipFileBuilder`

- `AddFile(name, byte[] | Stream | string)`. Add a single entry. The `string` overload uses UTF-8 by default.
- `AddFiles(Dictionary<string, byte[]>)` / `AddFiles(params string[] filePaths)`. Add multiple entries.
- `AddFileFromPath(path, entryName?)`. Add an entry from a file on disk.
- `AddDirectory(path, entryPrefix = "")`. Recursively add an entire directory tree.
- `Build()` / `BuildToFile(path)` / `BuildToStream()`. Build the archive (one-shot). The instance cannot be reused after building.

## `BulkEmailRequestBuilder`

Use for bulk sends with a shared default sender:

- `SetDefaultFrom(fromAddress, fromName)`. Set default sender for all messages.
- `SetMaxLimit(maxLimit)`. Set maximum number of messages allowed.
- `Add(to, subject, textBody?, htmlBody?)`. Add a message.
- `Add(to, subject, textBody, htmlBody, fromAddress?, fromName?)`. Add with per-message sender override.
- `AddCc(cc)` / `AddBcc(bcc)`. Add CC/BCC to the last message.
- `Clear()`. Clear all messages and default sender.
- `Build()`. Build the collection of EmailRequestBuilders (used internally by SendBulkEmailAsync).

```csharp
var bulk = BulkEmailRequestBuilder.New()
    .SetDefaultFrom("noreply@example.com", "My App")
    .Add("user1@example.com", "Subject 1", "Body 1")
    .Add("user2@example.com", "Subject 2", "Body 2", "<p>Body 2</p>");
var bulkResult = await _emailService.SendBulkEmailAsync(bulk);
```

## Thread safety

`EmailService` is thread-safe and can be registered as a singleton:

```csharp
services.AddSingleton<IEmailService, EmailService>();
```

Multiple threads can use the same instance concurrently.

## From address priority

- If `fromAddress` parameter is provided to `SendEmailAsync`, it overrides any From address in the builder
- If builder has a From address and no parameter is provided, the builder's From address is used
- If neither has a From address, the default From address from `EmailServiceOptions.DefaultFromAddress` and `EmailServiceOptions.DefaultFromName` is used

## Bulk email limits

- `MaxBulkEmailLimit` (default `1000`). `SendBulkEmailAsync` throws `ArgumentOutsideRangeException` if the input exceeds this count.
- `MaxAttachmentCountPerEmail` (default `20`). Enforced per request on both single and bulk sends.
- `BulkEmailConcurrencyLimit` (default `10`). A soft cap used by callers planning concurrent bulk batches. The current implementation processes messages sequentially within a single bulk call, so this value does not change in-call behavior.

## Cancellation

- `SendEmailAsync` operations return a failure result if cancelled
- `TestConnectionAsync` throws `OperationCanceledException` if cancelled
- Bulk operations check cancellation between emails and stop early if cancelled

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` (direct, lyo)
- `Lyo.Email.Models` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `MailKit` `4.17.0` (direct, third-party)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)