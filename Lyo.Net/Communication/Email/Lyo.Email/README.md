# Lyo.Email

MailKit SMTP sender. `EmailService` is the `IEmailService` implementation.

## Features

- **EmailRequestBuilder.** Fluent helper for assembling emails.
- **SMTP.** Delivery goes through MailKit.
- **Bulk.** Sequential batch send on one SMTP connection, with a result per message.
- **Attachments.** File attachments plus `ZipFileBuilder` to pack files into one ZIP.
- **HTML and text.** HTML bodies and plain-text bodies.
- **Results.** Failures come back as `Result<EmailRequest>` (`EmailResult` for a single send).
- **Logging.** Writes through Microsoft.Extensions.Logging.
- **Metrics.** Optional instrumentation for email operations.
- **DI.** `AddEmailService` plus `AddEmailServiceFromConfiguration`.
- **Async.** Every method accepts a `CancellationToken`.
- **Events.** `EmailSending`, `EmailSent`, `BulkSending`, `BulkEmailSent`, `ConnectionTested`.
- **Validation.** `EmailServiceOptionsValidator` verifies required options.

## Examples

### Set email options

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

### Set email options (2)

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

### Register from config

```csharp
// In ConfigureServices(context, services):
services.AddEmailServiceFromConfiguration(context.Configuration);
// Override the configuration section name if needed (defaults to "EmailServiceOptions"):
// services.AddEmailServiceFromConfiguration(context.Configuration, "MySection");
```

### Register via a callback

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

### Register from the service provider

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

### Register via a small callback

```csharp
services.AddEmailService(options => {
    options.Host = "smtp.example.com";
    options.Port = 587;
    options.DefaultFromAddress = "noreply@example.com";
    options.DefaultFromName = "My Application";
});
```

### Send a welcome message

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

### Plain email

```csharp
var builder = EmailRequestBuilder.New()
    .SetSubject("Hello")
    .SetTextBody("This is a test email")
    .AddTo("recipient@example.com", "Recipient Name");

var result = await _emailService.SendEmailAsync(builder);
```

### HTML-bodied email

```csharp
var builder = EmailRequestBuilder.New()
    .SetSubject("HTML Email")
    .SetHtmlBody("<h1>Hello</h1><p>This is an <strong>HTML</strong> email.</p>")
    .SetTextBody("Hello. This is an HTML email.") // Plain text fallback
    .AddTo("recipient@example.com");

var result = await _emailService.SendEmailAsync(builder);
```

### Mail with files attached

```csharp
var builder = EmailRequestBuilder.New()
    .SetSubject("Email with Attachment")
    .SetTextBody("Please find the attachment.")
    .AddTo("recipient@example.com")
    .AddAttachment("document.pdf", File.ReadAllBytes("path/to/document.pdf"));

var result = await _emailService.SendEmailAsync(builder);
```

### Several files in one ZIP

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

### Override the From address

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

### Batch email

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

### Check the connection

```csharp
var isConnected = await _emailService.TestConnectionAsync();
if (isConnected)
{
    Console.WriteLine("SMTP connection successful!");
}
```

### Listen for events

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
        Console.WriteLine($"Email sent successfully: {result.MessageId}");
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

## Set email options

#### From a configuration file (appsettings.json)

#### From code

## Add services

#### From configuration binding

#### From an action

#### From the service provider

#### From a minimal action

## Several files in one ZIP

`ZipFileBuilder` packs several files into one ZIP byte array that can be attached like any other file. `ZipFileBuilder` is a one-shot builder. After `Build()`/`BuildToFile()`/`BuildToStream()` the archive is closed and the instance cannot be reused.

## Resilience

Built-in retry or timeout logic is not included. Apply resilience at the application layer (for example using [Lyo.Resilience](https://www.nuget.org/packages/Lyo.Resilience)
or Polly) by wrapping calls to `IEmailService`:

```csharp
// Example: wrap email sends with IResilientExecutor
await _resilientExecutor.ExecuteAsync("email-pipeline", ct => _emailService.SendEmailAsync(builder, ct), cancellationToken);
```

## EmailServiceOptions

The configuration section name defaults to `EmailServiceOptions` (published as `EmailServiceOptions.SectionName`).

## Validation

- `Host` must not be null or empty
- `Port` must be between 1 and 65535
- `DefaultFromAddress` must not be null or empty
- `DefaultFromName` must not be null or empty
- `MaxAttachmentCountPerEmail` must be greater than 0

## Handling errors

Every email operation returns `Result<EmailRequest>` (runtime type `EmailResult` for single sends):

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

## `EmailResult` fields

- `IsSuccess`. Whether the operation succeeded.
- `Data`. The EmailRequest (recipients, subject, and related fields).
- `Errors`. Errors collected when the call failed.
- `MessageId`. SMTP message id (on EmailResult, when success).
- `SentDate`. When the email went out (on EmailResult, when success).
- `SmtpResponse`. SMTP server reply (on EmailResult, when success).

## Events

EmailService raises the following events:

## EmailSending

Raised before each email goes out (including during bulk runs):

## EmailSent

Raised after each email is sent (success or failure):

## BulkSending

Raised before a bulk email run starts:

## BulkEmailSent

Raised after a bulk email run finishes:

## ConnectionTested

Raised when a connection test finishes:

## Logging

Logging uses Microsoft.Extensions.Logging:

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

- `email.send.duration`. Timer covering send operations.
- `email.send.success`. Counts successful sends.
- `email.send.failure`. Counts failed sends.
- `email.send.cancelled`. Counts cancelled sends.
- `email.send.last_duration_ms`. Gauge of the last send duration.
- `email.bulk.send.duration`. Timer covering bulk operations.
- `email.bulk.send.total`. Counts total bulk emails.
- `email.bulk.send.success`. Counts successful bulk emails.
- `email.bulk.send.failure`. Counts failed bulk emails.
- `email.bulk.send.last_duration_ms`. Gauge of the last bulk duration.
- `email.smtp.connect.duration`. Time spent opening SMTP.
- `email.smtp.authenticate.duration`. Time spent on SMTP authentication.
- `email.test_connection.duration`. Time spent on a connection test.
- `email.test_connection.success`. Counts successful connection tests.
- `email.test_connection.failure`. Counts failed connection tests.

## `IEmailService`

- `Task<Result<EmailRequest>> SendEmailAsync(EmailRequestBuilder requestBuilder, string fromAddress, string? fromName = null, CancellationToken ct = default)`. Send mail using a custom from address.
- `Task<Result<EmailRequest>> SendEmailAsync(EmailRequestBuilder requestBuilder, CancellationToken ct = default)`. Send mail using the default from address.
- `Task<Result<EmailRequest>> SendEmailAsync(EmailRequest request, CancellationToken ct = default)`. Send mail from an EmailRequest object.
- `Task<IReadOnlyList<Result<EmailRequest>>> SendBulkEmailAsync(IEnumerable<EmailRequestBuilder> builders, CancellationToken ct = default)`. Send several emails one after another.
- `Task<BulkResult<EmailRequest>> SendBulkEmailAsync(BulkEmailRequestBuilder bulkRequestBuilder, CancellationToken ct = default)`. Send bulk mail through BulkEmailRequestBuilder.
- `Task<bool> TestConnectionAsync(CancellationToken ct = default)`. Test SMTP connection.

## `EmailRequestBuilder`

- `AddTo(...)`. Add To recipients.
- `AddCc(...)`. Add Cc recipients.
- `AddBcc(...)`. Add Bcc recipients.
- `SetFrom(...)`. Assign the From address.
- `SetReplyTo(...)`. Assign the Reply-To address.
- `SetSubject(...)`. Assign the email subject.
- `SetPriority(...)`. Assign message priority.
- `SetHtmlBody(...)`. Assign the HTML body.
- `SetTextBody(...)`. Assign the plain-text body.
- `AppendHtmlBody(...)`. Append onto the HTML body.
- `AppendTextBody(...)`. Append onto the text body.
- `AddAttachment(...)`. Attach files (use `ZipFileBuilder` first if you want a ZIP attachment).
- `AddHeader(...)`. Attach custom headers.
- `ClearTo()` / `ClearCc()` / `ClearBcc()` / `ClearAttachments()`. Empty the collections.
- `Build()`. Produce the MimeMessage.

## `ZipFileBuilder`

- `AddFile(name, byte[] | Stream | string)`. Add a single entry. The `string` overload defaults to UTF-8.
- `AddFiles(Dictionary<string, byte[]>)` / `AddFiles(params string[] filePaths)`. Add several entries.
- `AddFileFromPath(path, entryName?)`. Add one entry from a disk file.
- `AddDirectory(path, entryPrefix = "")`. Recursively add a whole directory tree.
- `Build()` / `BuildToFile(path)` / `BuildToStream()`. Build the archive (one-shot). The instance cannot be reused after a build.

## `BulkEmailRequestBuilder`

Meant for bulk sends with a shared default sender:

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

## Concurrent use

`EmailService` is thread-safe and can be wired as a singleton:

```csharp
services.AddSingleton<IEmailService, EmailService>();
```

The same instance can be used from multiple threads at once.

## Which From address wins

- If `fromAddress` parameter is provided to `SendEmailAsync`, it overrides any From address on the builder
- If the builder has a From address and no parameter is provided, the builder's From is used
- If neither supplies a From address, the default From from `EmailServiceOptions.DefaultFromAddress` and `EmailServiceOptions.DefaultFromName` is used

## Bulk-send caps

- `MaxBulkEmailLimit` (default `1000`). `SendBulkEmailAsync` throws `ArgumentOutsideRangeException` when the input exceeds this count.
- `MaxAttachmentCountPerEmail` (default `20`). Enforced per request for both single and bulk sends.
- `BulkEmailConcurrencyLimit` (default `10`). A soft cap used by callers planning concurrent bulk batches. The current implementation sends messages sequentially within a single bulk call, so this value does not change behavior inside a call.

## Cancellation

- `SendEmailAsync` returns a failure result when cancelled
- `TestConnectionAsync` throws `OperationCanceledException` when cancelled
- Bulk runs check cancellation between emails and exit early when cancelled

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Email.Models` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `MailKit` `4.17.0` (direct, third-party)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)