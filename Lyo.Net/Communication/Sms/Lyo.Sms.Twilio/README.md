# Lyo.Sms.Twilio

Twilio SMS and MMS through `Lyo.Sms`. `TwilioSmsService` implements `ISmsService`.

## Features

- **Twilio.** SMS and MMS through the Twilio API.
- **Bulk.** Bulk SMS with rate limiting.
- **MMS.** Up to 10 media attachments per message.
- **Query.** `GetMessagesAsync` with `SmsMessageQueryFilter`.
- **Errors.** Twilio-specific error codes on `TwilioSmsResult`.
- **Logging.** Logs through Microsoft.Extensions.Logging.
- **Metrics.** Optional metrics on SMS operations.
- **DI.** `AddTwilioSmsService` and `AddTwilioSmsServiceFromConfiguration`.
- **Async.** Methods take `CancellationToken`.
- **Concurrency.** Safe for concurrent use.
- **Validation.** `TwilioOptionsValidator` checks required options.
- **Events.** `MessageSending`, `MessageSent`, `BulkSending`, `BulkSent`.

## Examples

### Configure Twilio options

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

### Configure Twilio options (2)

```csharp
var options = new TwilioOptions
{
    AccountSid = "your_account_sid",
    AuthToken = "your_auth_token",
    DefaultFromPhoneNumber = "+1234567890",
    BulkSmsConcurrencyLimit = 10, // Max concurrent bulk SMS requests (default: 10)
    MaxMessageBodyLength = 1600, // Max message body length in characters (default: 1600)
    MaxBulkSmsLimit = 1000 // Max messages per bulk operation (default: 1000)
};
```

### Register services

```csharp
// In ConfigureServices(context, services):
services.AddTwilioSmsServiceFromConfiguration(context.Configuration);
// Override the configuration section name (default: "TwilioOptions"):
// services.AddTwilioSmsServiceFromConfiguration(context.Configuration, "MyTwilio");
```

### Send an SMS

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

### Send MMS

```csharp
var builder = SmsMessageBuilder
    .New()
    .SetTo("+1234567890")
    .SetFrom("+1987654321")
    .SetBody("Check out this image!")
    .AddMediaUrl(new Uri("https://example.com/image.jpg"));

var result = await _smsService.SendAsync(builder);
```

### Send bulk messages

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

### Query messages

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

### Get a message by id

```csharp
var message = await _smsService.GetMessageByIdAsync("SM1234567890abcdef");
if (message.IsSuccess)
{
    Console.WriteLine($"Status: {message.Status}");
    Console.WriteLine($"Body: {message.Body}");
    Console.WriteLine($"Price: {((TwilioSmsResult)message).Price} {((TwilioSmsResult)message).PriceUnit}");
}
```

### Test connection

```csharp
var isConnected = await _smsService.TestConnectionAsync();
if (isConnected)
{
    Console.WriteLine("Connected to Twilio!");
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
        if (result is TwilioSmsResult twilioResult)
        {
            Console.WriteLine($" Status: {twilioResult.Status}");
            Console.WriteLine($" Price: {twilioResult.Price} {twilioResult.PriceUnit}");
        }
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

### Subscribe to events

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
            if (args.SmsResult is TwilioSmsResult twilioResult)
            {
                Console.WriteLine($" Twilio Status: {twilioResult.Status}");
                Console.WriteLine($" Cost: {twilioResult.Price} {twilioResult.PriceUnit}");
            }
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
        
        if (bulkResult.FailureCount > 0)
        {
            Console.WriteLine($" Failures: {bulkResult.FailureCount}");
            foreach (var r in bulkResult.FailedResults)
                Console.WriteLine($" - {r.Data?.To}: {string.Join("; ", r.Errors?.Select(e => e.Message) ?? [])}");
        }
    }
}
```

### TwilioSmsResult

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

### Error handling

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

## Configure Twilio options

#### Using configuration file (appsettings.json)

#### Using code

## Register services

#### Using configuration binding

`AddTwilioSmsService(IConfiguration, string)` is an alias kept for callers that prefer the shorter name.
Both register the same singletons: `TwilioOptions`, `TwilioOptionsValidator`, `TwilioSmsService`, plus
the cross-typed `ISmsService` and `ISmsService<TwilioSmsResult>` interfaces backed by the same instance.
On `net6.0`+ targets an `IHttpClient` keyed `"lyo-twilio-sms"` is also registered so Twilio reuses the
shared `IHttpClientFactory` pool. Resilience policies on the application's `IHttpClientFactory`
configuration apply automatically.

## Send bulk messages

#### Using IEnumerable of builders

#### Using BulkSmsBuilder

## Events

The Twilio SMS service raises events around send operations:

#### MessageSending

Fired before each message is sent, including during bulk operations:

#### MessageSent

Fired after each message is sent (success or failure):

#### BulkSending

Fired before a bulk send starts:

#### BulkSent

Fired after a bulk send completes:

#### Complete event example

Events fire even when operations fail, so you can track every attempt. Useful for monitoring, logging, and user notifications.

## Twilio error codes

Twilio-specific error codes are included in the result via `TwilioSmsResult.TwilioErrorCode`:

```csharp
if (!result.IsSuccess && result is TwilioSmsResult twilioResult)
{
    if (twilioResult.TwilioErrorCode.HasValue)
    {
        Console.WriteLine($"Twilio Error Code: {twilioResult.TwilioErrorCode}");
        // Common error codes:
        // 20003 - Unreachable destination handset
        // 20429 - Too Many Requests (rate limit)
        // 30001 - Queue overflow
        // 30008 - Unknown destination handset
    }

    // The Errors collection (inherited from Result<SmsRequest>) carries human-readable messages and codes
    var firstError = twilioResult.Errors?.FirstOrDefault();
    Console.WriteLine($"Error: {firstError?.Message} ({firstError?.Code})");
}
```

## Resilience

The library does not include built-in retry or timeout logic. Apply resilience at the application layer (e.g. using [Lyo.Resilience](https://www.nuget.org/packages/Lyo.Resilience) with `AddLyoResilienceHandler` on the HttpClient, or by wrapping `ISmsService` calls) as needed.

## Rate limiting

- **Concurrent requests.** Limited to 10 concurrent requests (configurable via `BulkSmsConcurrencyLimit`).
- **Throttling.** `SemaphoreSlim` caps concurrency.
- **Async.** Methods take `CancellationToken`.
- **Bulk limits.** Maximum messages per bulk operation (configurable via `MaxBulkSmsLimit`).

## Thread safety

- All instance fields are readonly
- Bulk operations use thread-safe collections (`ConcurrentBag`)
- Rate limiting uses `SemaphoreSlim` for thread-safe concurrency control
- The underlying Twilio SDK client is thread-safe

## `TwilioOptions` properties

| Property | Type | Required | Default | Description |
| ------------------------- | --------- | -------- | ------- | ------------------------------------- |
| `AccountSid` | `string` | Yes | - | Your Twilio Account SID |
| `AuthToken` | `string` | Yes | - | Your Twilio Auth Token |
| `DefaultFromPhoneNumber` | `string?` | No | - | Default sender phone number |
| `BulkSmsConcurrencyLimit` | `int` | No | 10 | Max concurrent bulk SMS requests |
| `MaxMessageBodyLength` | `int` | No | 1600 | Max message body length in characters |
| `MaxBulkSmsLimit` | `int` | No | 1000 | Max messages per bulk operation |
| `EnableMetrics` | `bool` | No | false | Enable metrics collection |

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

- **Information.** Successful operations, message details
- **Warning.** Retries, long messages, connection issues
- **Error.** Failures, exceptions

Phone numbers are masked in logs (last 4 digits only).

## Metrics

Set `EnableMetrics` to collect:

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

- `AccountSid` is required
- `AuthToken` is required
- Validation runs via `services.AddOptions<TwilioOptions>().ValidateOnStart()` when using `AddTwilioSmsServiceFromConfiguration()` / `AddTwilioSmsService(IConfiguration, ...)`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Lyo.Sms` (direct, lyo)
- `Microsoft.Extensions.Http` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (direct, microsoft)
- `Twilio` `7.14.9` (direct, third-party)
- `Lyo.Common` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Sms.Models` (transitive, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)