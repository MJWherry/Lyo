# Lyo.Sms.Twilio

A production-ready Twilio SMS/MMS service implementation for .NET, built on the extensible `Lyo.Sms` library.

## Features

- ✅ **Twilio Integration** - Full support for Twilio SMS and MMS messaging
- ✅ **Bulk Messaging** - Efficient bulk SMS sending with rate limiting
- ✅ **MMS Support** - Send multimedia messages with up to 10 media attachments
- ✅ **Message Querying** - Query messages by various filter criteria
- ✅ **Error Handling** - Comprehensive error handling with Twilio-specific error codes
- ✅ **Logging** - Built-in logging support via Microsoft.Extensions.Logging
- ✅ **Metrics** - Optional metrics collection for monitoring SMS operations
- ✅ **Dependency Injection** - Full support for .NET dependency injection
- ✅ **Async/Await** - Fully asynchronous API with cancellation token support
- ✅ **Thread-Safe** - Thread-safe implementation for concurrent use
- ✅ **Validation** - Automatic validation of required configuration options
- ✅ **Events** - Events for message sending, message sent, bulk sending, and bulk sent

## Quick Start

### 1. Configure Twilio Options

#### Using Configuration File (appsettings.json)

```json
{
  "TwilioOptions": {
    "AccountSid": "your_account_sid",
    "AuthToken": "your_auth_token",
    "DefaultFromPhoneNumber": "+1234567890",
    "BulkSmsConcurrencyLimit": 10,
    "MaxMessageBodyLength": 1600,
    "MaxBulkSmsLimit": 1000,
    "EnableMetrics": false
  }
}
```

#### Using Code

```csharp
var options = new TwilioOptions
{
    AccountSid = "your_account_sid",
    AuthToken = "your_auth_token",
    DefaultFromPhoneNumber = "+1234567890",
    BulkSmsConcurrencyLimit = 10,      // Max concurrent bulk SMS requests (default: 10)
    MaxMessageBodyLength = 1600,       // Max message body length in characters (default: 1600)
    MaxBulkSmsLimit = 1000             // Max messages per bulk operation (default: 1000)
};
```

### 2. Register Services

#### Using Configuration Binding

```csharp
// In ConfigureServices(context, services):
services.AddTwilioSmsServiceViaConfiguration(context.Configuration);
```

### 3. Use the Service

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

## Usage Examples

### Using the Builder Pattern

```csharp
var builder = SmsMessageBuilder
    .New()
    .SetTo("+1234567890")
    .SetFrom("+1987654321")
    .SetBody("Hello, World!");

var result = await _smsService.SendAsync(builder);
```

### Sending MMS (Multimedia Messages)

```csharp
var builder = SmsMessageBuilder
    .New()
    .SetTo("+1234567890")
    .SetFrom("+1987654321")
    .SetBody("Check out this image!")
    .AddMediaUrl(new Uri("https://example.com/image.jpg"));

var result = await _smsService.SendAsync(builder);
```

### Sending Bulk Messages

#### Using IEnumerable of Builders

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

#### Using BulkSmsBuilder (Recommended)

```csharp
var bulkBuilder = BulkSmsBuilder
    .New()
    .SetDefaultFrom("+1987654321")  // Optional: set default sender for all messages
    .SetMaxLimit(100)                // Optional: limit number of messages
    .Add("+1111111111", "Message 1")
    .Add("+2222222222", "Message 2")
    .Add("+3333333333", "Message 3", "+19998887777");  // Override sender for specific message

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

### Querying Messages

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

### Getting a Message by ID

```csharp
var message = await _smsService.GetMessageByIdAsync("SM1234567890abcdef");
if (message.IsSuccess)
{
    Console.WriteLine($"Status: {message.Status}");
    Console.WriteLine($"Body: {message.Body}");
    Console.WriteLine($"Price: {((TwilioSmsResult)message).Price} {((TwilioSmsResult)message).PriceUnit}");
}
```

### Testing Connection

```csharp
var isConnected = await _smsService.TestConnectionAsync();
if (isConnected)
{
    Console.WriteLine("Connected to Twilio!");
}
```

### Using Events

The Twilio SMS service provides events for monitoring message operations:

#### MessageSending Event

Fired before each message is sent (including during bulk operations):

```csharp
_smsService.MessageSending += (sender, args) =>
{
    var request = args.SmsRequest;
    Console.WriteLine($"Sending SMS to {request.To}: {request.Body}");
};
```

#### MessageSent Event

Fired after each message is sent (success or failure):

```csharp
_smsService.MessageSent += (sender, args) =>
{
    var result = args.SmsResult;
    if (result.IsSuccess)
    {
        Console.WriteLine($"SMS sent successfully: {result.MessageId}");
        if (result is TwilioSmsResult twilioResult)
        {
            Console.WriteLine($"  Status: {twilioResult.Status}");
            Console.WriteLine($"  Price: {twilioResult.Price} {twilioResult.PriceUnit}");
        }
    }
    else
    {
        Console.WriteLine($"SMS failed: {result.ErrorMessage}");
    }
};
```

#### BulkSending Event

Fired before a bulk send operation starts:

```csharp
_smsService.BulkSending += (sender, args) =>
{
    Console.WriteLine($"Starting bulk send for {args.BulkSmsMessage.Count} messages");
};
```

#### BulkSent Event

Fired after a bulk send operation completes:

```csharp
_smsService.BulkSent += (sender, args) =>
{
    var bulkResult = args.BulkSmsResult;
    Console.WriteLine($"Bulk send completed:");
    Console.WriteLine($"  Total: {bulkResult.TotalCount}");
    Console.WriteLine($"  Success: {bulkResult.SuccessCount}");
    Console.WriteLine($"  Failure: {bulkResult.FailureCount}");
    Console.WriteLine($"  Elapsed: {bulkResult.ElapsedTime}");
};
```

#### Complete Event Example

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
            Console.WriteLine($"✓ SMS sent: {args.SmsResult.MessageId}");
            if (args.SmsResult is TwilioSmsResult twilioResult)
            {
                Console.WriteLine($"  Twilio Status: {twilioResult.Status}");
                Console.WriteLine($"  Cost: {twilioResult.Price} {twilioResult.PriceUnit}");
            }
        }
        else
        {
            Console.WriteLine($"✗ SMS failed: {args.SmsResult.ErrorMessage}");
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
        
        if (failureCount > 0)
        {
            Console.WriteLine($"  Failures: {failureCount}");
            foreach (var result in args.Results.Where(r => !r.IsSuccess))
            {
                Console.WriteLine($"    - {result.To}: {result.ErrorMessage}");
            }
        }
    }
}
```

**Note**: Events fire even when operations fail, allowing you to track all SMS operations regardless of success or
failure. This is useful for monitoring, logging, and user notifications.

## Twilio-Specific Features

### TwilioSmsResult

The `TwilioSmsResult` extends `SmsResult` with Twilio-specific information:

```csharp
var result = await _smsService.SendSmsAsync("+1234567890", "Hello");

if (result is TwilioSmsResult twilioResult)
{
    Console.WriteLine($"Message SID: {twilioResult.MessageId}");
    Console.WriteLine($"Status: {twilioResult.Status}");
    Console.WriteLine($"Segments: {twilioResult.NumSegments}");
    Console.WriteLine($"Price: {twilioResult.Price} {twilioResult.PriceUnit}");
    Console.WriteLine($"Account SID: {twilioResult.AccountSid}");
}
```

### Error Codes

Twilio-specific error codes are included in the result:

```csharp
if (!result.IsSuccess && result is TwilioSmsResult twilioResult)
{
    if (twilioResult.ErrorCode.HasValue)
    {
        Console.WriteLine($"Twilio Error Code: {twilioResult.ErrorCode}");
        // Common error codes:
        // 20003 - Unreachable destination handset
        // 20429 - Too Many Requests (rate limit)
        // 30001 - Queue overflow
        // 30008 - Unknown destination handset
    }
}
```

## Resilience

The library does not include built-in retry or timeout logic. Apply resilience at the application layer (e.g. using [Lyo.Resilience](https://www.nuget.org/packages/Lyo.Resilience)
with `AddLyoResilienceHandler` on the HttpClient, or by wrapping `ISmsService` calls) as needed.

## Rate Limiting

Bulk operations automatically use rate limiting to prevent overwhelming Twilio:

- **Concurrent Requests**: Limited to 10 concurrent requests (configurable via `BulkSmsConcurrencyLimit`)
- **Automatic Throttling**: Built-in semaphore-based throttling
- **Non-blocking**: Uses async/await for efficient resource usage
- **Bulk Limits**: Maximum number of messages per bulk operation (configurable via `MaxBulkSmsLimit`)

## Thread Safety

The `TwilioSmsService` is **thread-safe** and can be safely used from multiple threads concurrently:

- All instance fields are readonly
- Bulk operations use thread-safe collections (`ConcurrentBag`)
- Rate limiting uses `SemaphoreSlim` for thread-safe concurrency control
- The underlying Twilio SDK client is thread-safe

## Configuration Options

### TwilioOptions Properties

| Property                  | Type      | Required | Default | Description                           |
|---------------------------|-----------|----------|---------|---------------------------------------|
| `AccountSid`              | `string`  | Yes      | -       | Your Twilio Account SID               |
| `AuthToken`               | `string`  | Yes      | -       | Your Twilio Auth Token                |
| `DefaultFromPhoneNumber`  | `string?` | No       | -       | Default sender phone number           |
| `BulkSmsConcurrencyLimit` | `int`     | No       | 10      | Max concurrent bulk SMS requests      |
| `MaxMessageBodyLength`    | `int`     | No       | 1600    | Max message body length in characters |
| `MaxBulkSmsLimit`         | `int`     | No       | 1000    | Max messages per bulk operation       |
| `EnableMetrics`           | `bool`    | No       | false   | Enable metrics collection             |

## Error Handling

The library includes comprehensive error handling:

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
    }
}
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

- **Information**: Successful operations, message details
- **Warning**: Retries, long messages, connection issues
- **Error**: Failures, exceptions

Phone numbers are automatically masked in logs (only last 4 digits shown) for privacy.

## Metrics

Optional metrics collection is available:

```csharp
services.AddLyoMetrics();
services.AddTwilioSmsService(options =>
{
    options.EnableMetrics = true;
    // ... other options
});
```

Metrics tracked:

- `sms.twilio.send.duration`
- `sms.twilio.send.success`
- `sms.twilio.send.failure`
- `sms.twilio.bulk.send.duration`
- `sms.twilio.bulk.send.total`
- `sms.twilio.bulk.send.success`
- `sms.twilio.bulk.send.failure`
- `sms.twilio.bulk.send.last_duration_ms`
- `sms.twilio.api.get_message.duration`
- `sms.twilio.api.get_messages.duration`
- `sms.twilio.test_connection.duration`

## Validation

Options are automatically validated on startup:

- `AccountSid` is required
- `AuthToken` is required
- Validation occurs when using `AddTwilioSmsServiceViaConfiguration()` or during service creation

If validation fails, an `OptionsValidationException` or `InvalidOperationException` is thrown.




<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Sms.Twilio.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |
| `Microsoft.Extensions.Http` | `[10,)` |
| `Microsoft.Extensions.Logging.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Options` | `[10,)` |
| `Microsoft.Extensions.Options.ConfigurationExtensions` | `[10,)` |
| `Twilio` | `[7.14,)` |

### Project references

- `Lyo.Exceptions`
- `Lyo.Sms`

## Public API (generated)

Top-level `public` types in `*.cs` (*7*). Nested types and file-scoped namespaces may omit some entries.

- `Constants`
- `Extensions`
- `IsExternalInit`
- `Metrics`
- `TwilioOptions`
- `TwilioOptionsValidator`
- `TwilioSmsService`

<!-- LYO_README_SYNC:END -->

## License

[Your License Here]
