# Lyo.Sms

A production-ready SMS library for .NET with extensible architecture for multiple providers.

## Features

- ✅ **Clean API** - Fluent builder pattern for constructing messages
- ✅ **Phone Number Validation** - Automatic validation and normalization to E.164 format
- ✅ **Bulk Messaging** - Efficient bulk SMS sending with rate limiting and BulkSmsBuilder
- ✅ **Error Handling** - Comprehensive error handling with retry logic for transient failures
- ✅ **Custom Exceptions** - InvalidFormatException and ArgumentOutsideRangeException for better error messages
- ✅ **Logging** - Built-in logging support via Microsoft.Extensions.Logging
- ✅ **Dependency Injection** - Full support for .NET dependency injection
- ✅ **Async/Await** - Fully asynchronous API with cancellation token support
- ✅ **Message Querying** - Query messages by various filter criteria
- ✅ **Extensible Architecture** - Abstract base class (SmsServiceBase) for easy provider implementation
- ✅ **Configurable Limits** - Configurable bulk SMS limits, message length limits, and concurrency limits
- ✅ **Events** - Events for message sending, message sent, bulk sending, and bulk sent

## Quick Start

### 1. Configure Options

Each provider will have its own options class that inherits from `SmsServiceOptions`:

```csharp
var options = new ProviderOptions  // Replace with your provider's options class
{
    DefaultFromPhoneNumber = "+1234567890",
    BulkSmsConcurrencyLimit = 10,      // Max concurrent bulk SMS requests (default: 10)
    MaxMessageBodyLength = 1600,       // Max message body length in characters (default: 1600)
    MaxBulkSmsLimit = 1000             // Max messages per bulk operation (default: 1000)
};
```

### 2. Register Services

Register the provider-specific service using the provider's extension methods. Each provider will have its own
registration methods.

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
}
```

### Testing Connection

```csharp
var isConnected = await _smsService.TestConnectionAsync();
if (isConnected)
{
    Console.WriteLine("Connected to SMS service!");
}
```

### Using Events

The SMS service provides events for monitoring message operations:

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
    }
}
```

**Note**: Events fire even when operations fail, allowing you to track all SMS operations regardless of success or
failure.

## Phone Number Formats

The library supports multiple phone number formats and automatically normalizes them to E.164 format:

- **E.164**: `+1234567890` ✅
- **US Format**: `(555) 123-4567` ✅
- **US Format**: `555-123-4567` ✅
- **US Format**: `555.123.4567` ✅
- **US Format**: `5551234567` ✅ (assumes US country code +1)

## Message Limits

- **Maximum Length**: 1600 characters (10 segments of 160 characters each) - configurable via `MaxMessageBodyLength`
- Messages longer than 160 characters are automatically split into multiple segments
- The library validates message length before sending
- **Bulk SMS Limit**: Maximum number of messages per bulk operation (default: 1000) - configurable via `MaxBulkSmsLimit`
- **BulkSmsBuilder Limit**: Can set per-builder limit using `SetMaxLimit()` method

## Error Handling

The library includes comprehensive error handling with custom exception types:

- **Retry Logic**: Automatic retry with exponential backoff for transient errors
- **Error Codes**: Provider-specific error codes included in results
- **Exception Details**: Full exception information available in results
- **Logging**: All operations are logged for debugging
- **Custom Exceptions**:
    - `InvalidFormatException` - Thrown when phone number format is invalid (includes valid format examples)
    - `ArgumentOutsideRangeException` - Thrown when values are outside allowed ranges (e.g., message length)

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

### Exception Handling Examples

```csharp
try
{
    var builder = SmsMessageBuilder.New()
        .SetTo("invalid-phone")  // Will throw InvalidFormatException
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
        .SetBody(new string('A', 1601));  // Will throw ArgumentOutsideRangeException
}
catch (ArgumentOutsideRangeException ex)
{
    Console.WriteLine($"Message too long: {ex.ActualValue} characters");
    Console.WriteLine($"Maximum allowed: {ex.MaxValue} characters");
}
```

## Rate Limiting

Bulk operations automatically use rate limiting to prevent overwhelming the SMS provider:

- **Concurrent Requests**: Limited to 10 concurrent requests (configurable via `BulkSmsConcurrencyLimit`)
- **Automatic Throttling**: Built-in semaphore-based throttling
- **Non-blocking**: Uses async/await for efficient resource usage
- **Bulk Limits**: Maximum number of messages per bulk operation (configurable via `MaxBulkSmsLimit`)
- **Per-Builder Limits**: BulkSmsBuilder supports `SetMaxLimit()` to restrict messages at the builder level

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
- **Warning**: Retries, long messages
- **Error**: Failures, exceptions

## Testing

The library includes comprehensive unit tests. To run tests:

```bash
dotnet test
```

## Architecture

The library follows a clean architecture pattern with extensible base classes:

- **Lyo.Sms**: Core interfaces and models (provider-agnostic)
    - `ISmsService` - Main service interface
    - `SmsServiceBase` - Abstract base class providing common bulk SMS functionality
    - `SmsServiceOptions` - Base options class with common configuration properties
    - `SmsMessageBuilder` - Builder for individual messages
    - `BulkSmsBuilder` - Builder for bulk SMS operations
    - `SmsMessageQueryFilter` - Generic filter for querying messages
- **Provider Packages**: Provider-specific implementations (e.g., `Lyo.Sms.Twilio`)
    - Provider-specific service class - Inherits from `SmsServiceBase`, implements provider-specific methods
    - Provider-specific options class - Inherits from `SmsServiceOptions`, adds provider-specific properties

### Extending for New Providers

To add support for a new SMS provider, simply:

1. Create an options class inheriting from `SmsServiceOptions`:

```csharp
public class MyProviderOptions : SmsServiceOptions
{
    public string ApiKey { get; set; } = null!;
    public string ApiSecret { get; set; } = null!;
}
```

2. Create a service class inheriting from `SmsServiceBase`:

```csharp
public class MyProviderSmsService : SmsServiceBase
{
    private readonly MyProviderOptions _options;
    
    public MyProviderSmsService(MyProviderOptions options, ILogger<MyProviderSmsService> logger, IMetrics? metrics = null)
        : base(options, logger, metrics)
    {
        _options = options;
    }
    
    // Only need to implement these 4 methods:
    public override Task<SmsResult> SendSmsAsync(SmsMessage message, CancellationToken ct = default)
    {
        // Provider-specific send implementation
    }
    
    public override Task<SmsResult> GetMessageByIdAsync(string messageId, CancellationToken ct = default)
    {
        // Provider-specific get by ID implementation
    }
    
    public override Task<IEnumerable<SmsResult>> GetMessagesAsync(SmsMessageQueryFilter filter, CancellationToken ct = default)
    {
        // Provider-specific query implementation
    }
    
    public override Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        // Provider-specific connection test
    }
}
```

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

All bulk SMS operations, rate limiting, and common functionality are automatically provided by the base class!




<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Sms.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Logging.Abstractions` | `[10,)` |

### Project references

- `Lyo.Common`
- `Lyo.Exceptions`
- `Lyo.Metrics`
- `Lyo.Sms.Models`

## Public API (generated)

Top-level `public` types in `*.cs` (*8*). Nested types and file-scoped namespaces may omit some entries.

- `BulkSmsBuilder`
- `Constants`
- `IsExternalInit`
- `ISmsService`
- `Metrics`
- `SmsErrorCodes`
- `SmsMessageBuilder`
- `SmsServiceBase`

<!-- LYO_README_SYNC:END -->

## License

[Your License Here]
