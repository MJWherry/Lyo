# Lyo.Email

A production-ready email service library for .NET with SMTP support, built on MailKit.

## Features

- ✅ **Clean API** - Fluent builder pattern for constructing emails
- ✅ **SMTP Support** - Full SMTP support via MailKit
- ✅ **Bulk Sending** - Efficient bulk email sending with progress tracking
- ✅ **Attachments** - Support for file attachments, including ZIP compression
- ✅ **HTML & Text** - Support for both HTML and plain text email bodies
- ✅ **Error Handling** - Comprehensive error handling with detailed results
- ✅ **Logging** - Built-in logging support via Microsoft.Extensions.Logging
- ✅ **Metrics** - Optional metrics collection for monitoring email operations
- ✅ **Dependency Injection** - Full support for .NET dependency injection
- ✅ **Async/Await** - Fully asynchronous API with cancellation token support
- ✅ **Events** - Events for email sent, bulk completed, and connection tested
- ✅ **Validation** - Automatic validation of required configuration options

## Quick Start

### 1. Configure Email Options

#### Using Configuration File (appsettings.json)

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

#### Using Code

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

### 2. Register Services

#### Using Configuration Binding

```csharp
// In ConfigureServices(context, services):
services.AddEmailServiceViaConfiguration(context.Configuration);
```

#### Using Action

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

#### Using Service Provider

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

#### Using Action (minimal)

```csharp
services.AddEmailService(options => {
    options.Host = "smtp.example.com";
    options.Port = 587;
    options.DefaultFromAddress = "noreply@example.com";
    options.DefaultFromName = "My Application";
});
```

### 3. Use the Service

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

## Usage Examples

### Basic Email

```csharp
var builder = EmailRequestBuilder.New()
    .SetSubject("Hello")
    .SetTextBody("This is a test email")
    .AddTo("recipient@example.com", "Recipient Name");

var result = await _emailService.SendEmailAsync(builder);
```

### HTML Email

```csharp
var builder = EmailRequestBuilder.New()
    .SetSubject("HTML Email")
    .SetHtmlBody("<h1>Hello</h1><p>This is an <strong>HTML</strong> email.</p>")
    .SetTextBody("Hello. This is an HTML email.") // Plain text fallback
    .AddTo("recipient@example.com");

var result = await _emailService.SendEmailAsync(builder);
```

### Email with Attachments

```csharp
var builder = EmailRequestBuilder.New()
    .SetSubject("Email with Attachment")
    .SetTextBody("Please find the attachment.")
    .AddTo("recipient@example.com")
    .AddAttachment("document.pdf", File.ReadAllBytes("path/to/document.pdf"));

var result = await _emailService.SendEmailAsync(builder);
```

### Multiple Attachments as ZIP

```csharp
var files = new Dictionary<string, byte[]>
{
    { "file1.txt", Encoding.UTF8.GetBytes("Content 1") },
    { "file2.txt", Encoding.UTF8.GetBytes("Content 2") }
};

var builder = EmailRequestBuilder.New()
    .SetSubject("Files Attached")
    .SetTextBody("Please find the attached files.")
    .AddTo("recipient@example.com")
    .AddAttachmentsAsZip("files.zip", files);

var result = await _emailService.SendEmailAsync(builder);
```

### Custom From Address

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

### Bulk Email Sending

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

### Testing Connection

```csharp
var isConnected = await _emailService.TestConnectionAsync();
if (isConnected)
{
    Console.WriteLine("SMTP connection successful!");
}
```

### Using Events

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

## Resilience

The library does not include built-in retry or timeout logic. Apply resilience at the application layer (e.g. using [Lyo.Resilience](https://www.nuget.org/packages/Lyo.Resilience)
or Polly) by wrapping calls to `IEmailService`:

```csharp
// Example: wrap email sends with IResilientExecutor
await _resilientExecutor.ExecuteAsync("email-pipeline", ct => _emailService.SendEmailAsync(builder, ct), cancellationToken);
```

## Configuration

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
}
```

### Validation

The library automatically validates required options:

- `Host` must not be null or empty
- `Port` must be between 1 and 65535
- `DefaultFromAddress` must not be null or empty
- `DefaultFromName` must not be null or empty

Invalid options will throw `OptionsValidationException` at startup when using `AddEmailServiceViaConfiguration()`.

## Error Handling

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

### Result<EmailRequest> / EmailResult Properties

- `IsSuccess` - Whether the operation succeeded
- `Data` - The EmailRequest (recipients, subject, etc.)
- `Errors` - List of errors if failed
- `MessageId` - SMTP message ID (on EmailResult, when success)
- `SentDate` - When the email was sent (on EmailResult, when success)
- `SmtpResponse` - SMTP server response (on EmailResult, when success)

## Events

The email service provides events for monitoring email operations:

### EmailSending Event

Fired before each email is sent (including during bulk operations):

```csharp
_emailService.EmailSending += (sender, args) =>
{
    var request = args.EmailRequest;
    Console.WriteLine($"Sending email to {string.Join(", ", request.ToAddresses ?? [])}: {request.Subject}");
};
```

### EmailSent Event

Fired after each email is sent (success or failure):

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

### BulkSending Event

Fired before a bulk email operation starts:

```csharp
_emailService.BulkSending += (sender, args) =>
{
    Console.WriteLine($"Starting bulk send for {args.BulkEmailMessage.Count} emails");
};
```

### BulkEmailSent Event

Fired after a bulk email operation completes:

```csharp
_emailService.BulkEmailSent += (sender, args) =>
{
    var bulkResult = args.BulkEmailResult;
    Console.WriteLine($"Bulk send completed:");
    Console.WriteLine($"  Total: {bulkResult.TotalCount}");
    Console.WriteLine($"  Success: {bulkResult.SuccessCount}");
    Console.WriteLine($"  Failure: {bulkResult.FailureCount}");
};
```

### ConnectionTested Event

Fired when a connection test completes:

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

## Logging

The library uses Microsoft.Extensions.Logging for all logging:

```csharp
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});
```

Log levels:

- **Information**: Successful operations, email details
- **Debug**: SMTP connection details, authentication
- **Warning**: Cancellations, disconnection errors
- **Error**: Failures, exceptions

## Metrics

When `EnableMetrics` is set to `true` and an `IMetrics` service is registered, the library collects:

- `email.send.duration` - Duration timer for send operations
- `email.send.success` - Counter for successful sends
- `email.send.failure` - Counter for failed sends
- `email.send.cancelled` - Counter for cancelled sends
- `email.send.last_duration_ms` - Gauge for last send duration
- `email.bulk.send.duration` - Duration timer for bulk operations
- `email.bulk.send.total` - Counter for total bulk emails
- `email.bulk.send.success` - Counter for successful bulk emails
- `email.bulk.send.failure` - Counter for failed bulk emails
- `email.bulk.send.last_duration_ms` - Gauge for last bulk duration
- `email.smtp.connect.duration` - SMTP connection duration
- `email.smtp.authenticate.duration` - SMTP authentication duration
- `email.test_connection.duration` - Connection test duration
- `email.test_connection.success` - Counter for successful connection tests
- `email.test_connection.failure` - Counter for failed connection tests

## Testing

The library includes comprehensive unit tests. To run tests:

```bash
dotnet test
```

## API Reference

### IEmailService

- `Task<Result<EmailRequest>> SendEmailAsync(EmailRequestBuilder requestBuilder, string fromAddress, string? fromName = null, CancellationToken ct = default)` - Send email with
  custom from address

- `Task<Result<EmailRequest>> SendEmailAsync(EmailRequestBuilder requestBuilder, CancellationToken ct = default)` - Send email with default from address

- `Task<Result<EmailRequest>> SendEmailAsync(EmailRequest request, CancellationToken ct = default)` - Send email using EmailRequest object

- `Task<IReadOnlyList<Result<EmailRequest>>> SendBulkEmailAsync(IEnumerable<EmailRequestBuilder> builders, CancellationToken ct = default)` - Send multiple emails sequentially

- `Task<BulkResult<EmailRequest>> SendBulkEmailAsync(BulkEmailRequestBuilder bulkRequestBuilder, CancellationToken ct = default)` - Send bulk emails using BulkEmailRequestBuilder

- `Task<bool> TestConnectionAsync(CancellationToken ct = default)` - Test SMTP connection

### EmailRequestBuilder

- `AddTo(...)` - Add To recipients
- `AddCc(...)` - Add Cc recipients
- `AddBcc(...)` - Add Bcc recipients
- `SetFrom(...)` - Set From address
- `SetReplyTo(...)` - Set Reply-To address
- `SetSubject(...)` - Set email subject
- `SetPriority(...)` - Set message priority
- `SetHtmlBody(...)` - Set HTML body
- `SetTextBody(...)` - Set plain text body
- `AppendHtmlBody(...)` - Append to HTML body
- `AppendTextBody(...)` - Append to text body
- `AddAttachment(...)` - Add file attachments
- `AddAttachmentsAsZip(...)` - Add multiple files as ZIP
- `AddHeader(...)` - Add custom headers
- `ClearTo()` / `ClearCc()` / `ClearBcc()` / `ClearAttachments()` - Clear collections
- `Build()` - Build the MimeMessage

### BulkEmailRequestBuilder

Use for bulk sends with a shared default sender:

- `SetDefaultFrom(fromAddress, fromName)` - Set default sender for all messages
- `SetMaxLimit(maxLimit)` - Set maximum number of messages allowed
- `Add(to, subject, textBody?, htmlBody?)` - Add a message
- `Add(to, subject, textBody, htmlBody, fromAddress?, fromName?)` - Add with per-message sender override
- `AddCc(cc)` / `AddBcc(bcc)` - Add CC/BCC to the last message
- `Clear()` - Clear all messages and default sender
- `Build()` - Build the collection of EmailRequestBuilders (used internally by SendBulkEmailAsync)

```csharp
var bulk = BulkEmailRequestBuilder.New()
    .SetDefaultFrom("noreply@example.com", "My App")
    .Add("user1@example.com", "Subject 1", "Body 1")
    .Add("user2@example.com", "Subject 2", "Body 2", "<p>Body 2</p>");
var bulkResult = await _emailService.SendBulkEmailAsync(bulk);
```

## Thread Safety

The `EmailService` is **thread-safe** and can be registered as a **singleton**:

```csharp
services.AddSingleton<IEmailService, EmailService>();
```

Multiple threads can safely use the same instance concurrently.

## Important Notes

### From Address Priority

1. If `fromAddress` parameter is provided to `SendEmailAsync`, it overrides any From address in the builder
2. If builder has a From address and no parameter is provided, the builder's From address is used
3. If neither has a From address, the default From address from `EmailServiceOptions.DefaultFromAddress` and
   `EmailServiceOptions.DefaultFromName` is used

### Bulk Email Processing

Bulk emails are sent **sequentially**, not in parallel. Each email is sent one after another. If cancellation is
requested, processing stops and partial results are returned.

### Cancellation

- `SendEmailAsync` operations return a failure result if cancelled
- `TestConnectionAsync` throws `OperationCanceledException` if cancelled
- Bulk operations check cancellation between emails and stop early if cancelled




<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Email.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `MailKit` | `[4.15,)` |
| `Microsoft.Extensions.Logging.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Options.ConfigurationExtensions` | `[10,)` |

### Project references

- `Lyo.Common`
- `Lyo.Email.Models`
- `Lyo.Exceptions`
- `Lyo.Metrics`

## Public API (generated)

Top-level `public` types in `*.cs` (*9*). Nested types and file-scoped namespaces may omit some entries.

- `BulkEmailRequestBuilder`
- `Constants`
- `EmailRequestBuilder`
- `EmailService`
- `Extensions`
- `IEmailService`
- `IsExternalInit`
- `Metrics`
- `ZipFileBuilder`

<!-- LYO_README_SYNC:END -->

## License

[Your License Here]

---

**Production Ready:** This library has been reviewed for production use and includes:

- ✅ Thread-safe operations
- ✅ Comprehensive error handling
- ✅ Consumer-applied resilience (retry, timeout, etc.)
- ✅ Input validation and configuration validation
- ✅ Extensive test coverage
- ✅ Logging and metrics support
- ✅ Cancellation token support
- ✅ Event notifications for monitoring

