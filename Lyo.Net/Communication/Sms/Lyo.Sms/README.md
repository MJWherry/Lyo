# Lyo.Sms

Shared SMS contracts and send pipeline. Providers (`Lyo.Sms.Twilio`, and others) implement `SmsServiceBase`.

## Features

- **SmsMessageBuilder.** Fluent helper for assembling messages.
- **Phone numbers.** Validate and normalize toward E.164.
- **Bulk.** Batch send with rate limiting and `BulkSmsBuilder`.
- **Results.** Failures return `Result<SmsRequest>` (bulk: `BulkResult<SmsRequest>`). Add retries or timeouts in the caller. See provider packages — the Twilio README covers resilience.
- **Exceptions.** `InvalidFormatException` plus `ArgumentOutsideRangeException`.
- **Logging.** Writes through Microsoft.Extensions.Logging.
- **DI.** Provider packages register `ISmsService`.
- **Async.** Every method accepts a `CancellationToken`.
- **Query.** `GetMessagesAsync` filtered by `SmsMessageQueryFilter`.
- **SmsServiceBase.** Abstract base class for new providers.
- **Limits.** Configurable batch caps, message length, and concurrency.
- **Events.** `MessageSending`, `MessageSent`, `BulkSending`, `BulkSent`.

## Examples

### Set options

```csharp
var options = new ProviderOptions // Replace with your provider's options class
{
    DefaultFromPhoneNumber = "+1234567890",
    BulkSmsConcurrencyLimit = 10, // Max concurrent bulk SMS requests (default: 10)
    MaxMessageBodyLength = 1600, // Max message body length in characters (default: 1600)
    MaxBulkSmsLimit = 1000 // Max messages per bulk operation (default: 1000)
};
```

### Send one SMS

```csharp
public class MyService
{
    private readonly ISmsService _smsService;
    
    public MyService(ISmsService smsService)
    {
        _smsService = smsService;
    }
    
    public async Task SendSmsAsync()
    {
        // Simple send
        var result = await _smsService.SendSmsAsync(
            to: "+1234567890",
            body: "Hello, World!",
            from: "+1987654321"
        );
        
        if (result.IsSuccess)
        {
            Console.WriteLine($"Message sent! ID: {result.MessageId}");
        }
        else
        {
            Console.WriteLine($"Failed: {result.ErrorMessage}");
        }
    }
}
```

### SmsMessageBuilder

```csharp
var builder = SmsMessageBuilder
    .New()
    .SetTo("+1234567890")
    .SetFrom("+1987654321")
    .SetBody("Hello, World!");

var result = await _smsService.SendAsync(builder);
```

### Send a batch

```csharp
var messages = new[]
{
    SmsMessageBuilder.New().SetTo("+1111111111").SetBody("Message 1"),
    SmsMessageBuilder.New().SetTo("+2222222222").SetBody("Message 2"),
    SmsMessageBuilder.New().SetTo("+3333333333").SetBody("Message 3")
};

var results = await _smsService.SendBulkAsync(messages);

foreach (var result in results)
{
    if (result.IsSuccess)
    {
        Console.WriteLine($"Sent to {result.To}: {result.MessageId}");
    }
}
```

### BulkSmsBuilder

```csharp
var bulkBuilder = BulkSmsBuilder
    .New()
    .SetDefaultFrom("+1987654321") // Optional: set default sender for all messages
    .SetMaxLimit(100) // Optional: limit number of messages
    .Add("+1111111111", "Message 1")
    .Add("+2222222222", "Message 2")
    .Add("+3333333333", "Message 3", "+19998887777"); // Override sender for specific message

var bulkResult = await _smsService.SendBulkAsync(bulkBuilder);

Console.WriteLine($"Total: {bulkResult.TotalCount}");
Console.WriteLine($"Success: {bulkResult.SuccessCount}");
Console.WriteLine($"Failed: {bulkResult.FailureCount}");
Console.WriteLine($"Elapsed: {bulkResult.ElapsedTime}");

if (bulkResult.IsCompleteSuccess)
{
    Console.WriteLine("All messages sent successfully!");
}

foreach (var result in bulkResult.Results)
{
    if (result.IsSuccess)
    {
        Console.WriteLine($"Sent to {result.To}: {result.MessageId}");
    }
    else
    {
        Console.WriteLine($"Failed to send to {result.To}: {result.ErrorMessage}");
    }
}
```

### List messages

```csharp
var filter = new SmsMessageQueryFilter
{
    From = "+1987654321",
    DateSentAfter = DateTime.UtcNow.AddDays(-7),
    PageSize = 50
};

var result = await _smsService.GetMessagesAsync(filter);
foreach (var message in result.Items)
{
    Console.WriteLine($"{message.DateSent}: {message.Body}");
}
// Cursor-based pagination: use result.NextCursor as DateSentBefore for next page when result.HasMore
```

### Fetch one message by id

```csharp
var message = await _smsService.GetMessageByIdAsync("SM1234567890abcdef");
if (message.IsSuccess)
{
    Console.WriteLine($"Status: {message.Status}");
    Console.WriteLine($"Body: {message.Body}");
}
```

### Check the connection

```csharp
var isConnected = await _smsService.TestConnectionAsync();
if (isConnected)
{
    Console.WriteLine("Connected to SMS service!");
}
```

### MessageSending

```csharp
_smsService.MessageSending += (sender, args) =>
{
    var request = args.SmsRequest;
    Console.WriteLine($"Sending SMS to {request.To}: {request.Body}");
};
```

### MessageSent

```csharp
_smsService.MessageSent += (sender, args) =>
{
    var result = args.SmsResult;
    if (result.IsSuccess)
    {
        Console.WriteLine($"SMS sent successfully: {result.MessageId}");
    }
    else
    {
        Console.WriteLine($"SMS failed: {result.ErrorMessage}");
    }
};
```

### BulkSending

```csharp
_smsService.BulkSending += (sender, args) =>
{
    Console.WriteLine($"Starting bulk send for {args.BulkSmsMessage.Count} messages");
};
```

### BulkSent

```csharp
_smsService.BulkSent += (sender, args) =>
{
    var bulkResult = args.BulkSmsResult;
    Console.WriteLine($"Bulk send completed:");
    Console.WriteLine($" Total: {bulkResult.TotalCount}");
    Console.WriteLine($" Success: {bulkResult.SuccessCount}");
    Console.WriteLine($" Failure: {bulkResult.FailureCount}");
    Console.WriteLine($" Elapsed: {bulkResult.ElapsedTime}");
};
```

### Listen for events

```csharp
public class SmsNotificationService
{
    private readonly ISmsService _smsService;
    
    public SmsNotificationService(ISmsService smsService)
    {
        _smsService = smsService;
        SubscribeToEvents();
    }
    
    private void SubscribeToEvents()
    {
        _smsService.MessageSending += OnMessageSending;
        _smsService.MessageSent += OnMessageSent;
        _smsService.BulkSending += OnBulkSending;
        _smsService.BulkSent += OnBulkSent;
    }
    
    private void OnMessageSending(object? sender, SmsSendingEventArgs args)
    {
        Console.WriteLine($"Preparing to send SMS to {args.SmsRequest.To}");
    }
    
    private void OnMessageSent(object? sender, SmsSentEventArgs args)
    {
        if (args.SmsResult.IsSuccess)
        {
            Console.WriteLine($" SMS sent: {args.SmsResult.MessageId}");
        }
        else
        {
            Console.WriteLine($" SMS failed: {args.SmsResult.ErrorMessage}");
        }
    }
    
    private void OnBulkSending(object? sender, SmsBulkSendingEventArgs args)
    {
        Console.WriteLine($"Starting bulk SMS operation: {args.BulkSmsMessage.Count} messages");
    }
    
    private void OnBulkSent(object? sender, BulkSmsSentEventArgs args)
    {
        var bulkResult = args.BulkSmsResult;
        Console.WriteLine($"Bulk SMS completed: {bulkResult.SuccessCount}/{bulkResult.TotalCount} successful in {bulkResult.ElapsedTime.TotalSeconds:F2}s");
    }
}
```

### Handling exceptions

```csharp
try
{
    var builder = SmsMessageBuilder.New()
        .SetTo("invalid-phone") // Will throw InvalidFormatException
        .SetBody("Test");
}
catch (InvalidFormatException ex)
{
    Console.WriteLine($"Invalid phone number: {ex.InvalidValue}");
    Console.WriteLine($"Expected formats: {string.Join(", ", ex.ValidFormats)}");
}

try
{
    var builder = SmsMessageBuilder.New()
        .SetTo("+1234567890")
        .SetBody(new string('A', 1601)); // Will throw ArgumentOutsideRangeException
}
catch (ArgumentOutsideRangeException ex)
{
    Console.WriteLine($"Message too long: {ex.ActualValue} characters");
    Console.WriteLine($"Maximum allowed: {ex.MaxValue} characters");
}
```

### Testing

```bash
dotnet test
```

## Set options

Each provider has its own options class that inherits from `SmsServiceOptions`:

## Add services

Register the provider-specific service using the provider's extension methods. Each provider ships its own registration methods.

## Calling the service

The contract uses `ISmsService<TResult>` where `TResult : Result<SmsRequest>`. `ISmsService` is shorthand for `ISmsService<Result<SmsRequest>>`. Twilio uses `TwilioSmsResult` as `TResult` when provider-specific fields are needed.

## Send a batch

#### From an IEnumerable of builders

#### From BulkSmsBuilder

## Events

The SMS service raises events around send operations:

#### MessageSending

Raised before each message is sent, including during bulk operations:

#### MessageSent

Raised after each message is sent (success or failure):

#### BulkSending

Raised before a bulk send starts:

#### BulkSent

Raised after a bulk send completes:

#### Complete event example

Events still fire when operations fail, so you can track every attempt.

## Accepted phone formats

- **E.164.** `+1234567890`
- **US.** `(555) 123-4567`
- **US.** `555-123-4567`
- **US.** `555.123.4567`
- **US.** `5551234567` (assumes US country code +1)

## Message size limits

- **Maximum length.** 1600 characters (10 segments of 160 characters each), set via `MaxMessageBodyLength`.
- Bodies longer than 160 characters are split across segments.
- Message length is checked before send.
- **Bulk SMS limit.** Maximum messages per bulk operation (default 1000), set via `MaxBulkSmsLimit`.
- **BulkSmsBuilder limit.** Per-builder cap from `SetMaxLimit()`.

## Handling errors

The stack yields structured `Result` errors and validates inputs early (builders / normalization). Providers may attach error codes on specialized result types.

- **No built-in retries.** Callers or HTTP layers should implement policy if needed.
- **Error codes.** `SmsErrorCodes` (in `Lyo.Sms`) attaches the following constants to failed results raised by `SmsServiceBase`:

| Constant | Value | Raised when |
| -------------------- | --------------------- | -------------------------------------------------------- |
| `BuildFailed` | `BUILD_FAILED` | A builder threw while assembling the request. |
| `MessageNotBuilt` | `MESSAGE_NOT_BUILT` | The bulk pipeline hit send without a built request. |
| `OperationCancelled` | `OPERATION_CANCELLED` | The bulk send was cancelled through `CancellationToken`. |
| `MissingFromNumber` | `MISSING_FROM_NUMBER` | No `From` number was supplied or configured. |

Providers hang their own codes on derived result types (for example `TwilioSmsResult.TwilioErrorCode`).
- **Exception details.** Full exception information is available in results.
- **Logging.** Operations are logged.
- **Custom exceptions:**
    - `InvalidFormatException`. Thrown when a phone number format is invalid (includes valid format examples).
    - `ArgumentOutsideRangeException`. Thrown when values are outside allowed ranges (for example message length).

```csharp
var result = await _smsService.SendSmsAsync("+1234567890", "Hello");

if (!result.IsSuccess)
{
    Console.WriteLine($"Error: {result.ErrorMessage}");
    Console.WriteLine($"Error Code: {result.ErrorCode}");
    if (result.Exception != null)
    {
        Console.WriteLine($"Exception: {result.Exception.Message}");
        
        // Handle specific exception types
        if (result.Exception is InvalidFormatException formatEx)
        {
            Console.WriteLine($"Invalid Value: {formatEx.InvalidValue}");
            Console.WriteLine($"Valid Formats: {string.Join(", ", formatEx.ValidFormats)}");
        }
        else if (result.Exception is ArgumentOutsideRangeException rangeEx)
        {
            Console.WriteLine($"Value: {rangeEx.ActualValue}, Range: [{rangeEx.MinValue}, {rangeEx.MaxValue}]");
        }
    }
}
```

## Throughput limits

- **Concurrent requests.** Capped at 10 concurrent requests (configurable via `BulkSmsConcurrencyLimit`).
- **Throttling.** A `SemaphoreSlim` caps concurrency.
- **Async.** Every method accepts a `CancellationToken`.
- **Bulk limits.** Maximum messages per bulk operation (set via `MaxBulkSmsLimit`).
- **Per-builder limits.** `BulkSmsBuilder` supports `SetMaxLimit()` to cap messages at the builder level.

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

- **Information.** Successful operations, message details
- **Warning.** Retries, long messages
- **Error.** Failures, exceptions

## Metrics

`SmsServiceBase` emits its counters/timers under the keys exposed by `Constants.Metrics`. Providers replace
`CreateMetricNamesDictionary()` to prefix them with their own namespace (Twilio uses `sms.twilio.*`).

| Constant key (`Lyo.Sms.Constants.Metrics`) | Metric name | Kind |
| ------------------------------------------ | -------------------------------- | ------- |
| `SendDuration` | `sms.send.duration` | Timer |
| `SendSuccess` | `sms.send.success` | Counter |
| `SendFailure` | `sms.send.failure` | Counter |
| `BulkSendDuration` | `sms.bulk.send.duration` | Timer |
| `BulkSendTotal` | `sms.bulk.send.total` | Counter |
| `BulkSendSuccess` | `sms.bulk.send.success` | Counter |
| `BulkSendFailure` | `sms.bulk.send.failure` | Counter |
| `BulkSendLastDurationMs` | `sms.bulk.send.last_duration_ms` | Gauge |

## MMS helpers

`ISmsService<TResult>` exposes typed MMS convenience methods that wrap the builder:

```csharp
// Pass URLs as strings (validated/converted to Uri internally)
await _smsService.SendMmsAsync(
    to: "+1234567890",
    mediaUrls: ["https://example.com/image.jpg"],
    body: "Check this out");

// Or as System.Uri instances
await _smsService.SendMmsAsync(
    to: "+1234567890",
    mediaUrls: [new Uri("https://example.com/image.jpg")]);
```

The two overloads enforce `to`/`mediaUrls` (must not be null/empty), apply `DefaultFromPhoneNumber` when `from`
is omitted, and route through the same `SendCoreAsync` path as `SendSmsAsync`.

## Architecture

- **Lyo.Sms.** Core interfaces and models (provider-neutral).
- `ISmsService`. Primary service interface.
- `SmsServiceBase`. Abstract base that owns shared bulk SMS behavior.
- `SmsServiceOptions`. Base options holding shared settings.
- `SmsMessageBuilder`. Builder for one message.
- `BulkSmsBuilder`. Builder for batch SMS work.
- `SmsMessageQueryFilter`. Filter used when listing messages.
- **Provider packages.** Provider-specific implementations (such as `Lyo.Sms.Twilio`).
- The provider service class inherits from `SmsServiceBase` and implements provider methods.
- The provider options class inherits from `SmsServiceOptions` and adds provider properties.

## Adding a new provider

Adding a new SMS provider:

1. Create an options class inheriting from `SmsServiceOptions`:

```csharp
public class MyProviderOptions : SmsServiceOptions
{
    public string ApiKey { get; set; } = null!;
    public string ApiSecret { get; set; } = null!;
}
```

2. Implement `SmsServiceBase<TResult>`. Override `SendCoreAsync` (the provider call after `SmsRequest` is built), `GetMessageByIdAsync`, `GetMessagesAsync`, `TestConnectionCoreAsync`, and `CreateFailure`. Everything else on `ISmsService` (`SendSmsAsync`, `SendBulkAsync`, events, concurrency throttling, metrics hooks) stays in the base.

```csharp
public class MyProviderSmsService : SmsServiceBase<Result<SmsRequest>>
{
    public MyProviderSmsService(MyProviderOptions options, ILogger<MyProviderSmsService>? logger = null, IMetrics? metrics = null)
        : base(options, logger, metrics)
    {
    }

    protected override Task<Result<SmsRequest>> SendCoreAsync(SmsRequest request, CancellationToken ct)
        => Task.FromResult(Result<SmsRequest>.Failure("Not implemented", "sms.myprovider"));

    public override Task<Result<SmsRequest>> GetMessageByIdAsync(string messageId, CancellationToken ct = default)
        => Task.FromResult(Result<SmsRequest>.Failure("Not implemented", "sms.myprovider"));

    public override Task<SmsMessageQueryResults<Result<SmsRequest>>> GetMessagesAsync(SmsMessageQueryFilter filter, CancellationToken ct = default)
        => Task.FromResult(new SmsMessageQueryResults<Result<SmsRequest>>([], filter.PageSize, false));

    protected override Task<bool> TestConnectionCoreAsync(CancellationToken ct = default) => Task.FromResult(false);

    protected override Result<SmsRequest> CreateFailure(Exception exception, string code, SmsRequest? request = null)
        => Result<SmsRequest>.Failure(exception, code);
}
```

Richer `TResult` types (Twilio `TwilioSmsResult`) substitute `SmsServiceBase<TwilioSmsResult>`. See [`Lyo.Sms.Twilio`](../Lyo.Sms.Twilio/README.md).

3. Create extension methods for dependency injection:

```csharp
public static class Extensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddMyProviderSmsService(Action<MyProviderOptions> configure)
        {
            // Register options and service
            // Register ISmsService interface
        }
        
        public IServiceCollection AddMyProviderSmsServiceViaConfiguration(string configSectionName = "MyProviderOptions")
        {
            // Register via configuration binding
            // Register ISmsService interface
        }
    }
}
```

`SmsServiceBase` already implements bulk send, rate limiting, events, and metrics.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Lyo.Sms.Models` (direct, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)