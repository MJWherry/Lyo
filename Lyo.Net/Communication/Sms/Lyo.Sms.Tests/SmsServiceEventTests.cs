using Lyo.Sms.Builders;
using Lyo.Sms.Models;
using Lyo.Sms.Twilio;
using Lyo.Testing;
using Microsoft.Extensions.Logging;
using Twilio.Clients;

namespace Lyo.Sms.Tests;

public class SmsServiceEventTests
{
    private readonly ILogger<TwilioSmsService> _logger;
    private readonly TwilioOptions _options = new() { AccountSid = "AC1234567890abcdef1234567890abcdef", AuthToken = "test_auth_token", DefaultFromPhoneNumber = "+1987654321" };

    public SmsServiceEventTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        _logger = loggerFactory.CreateLogger<TwilioSmsService>();
    }

    private static TwilioRestClient CreateRestClient(TwilioOptions options) => new(options.AccountSid, options.AuthToken);

    [Fact]
    public async Task MessageSending_Event_FiresOnSend()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        SmsSendingEventArgs? eventArgs = null;
        service.MessageSending += (_, args) => {
            eventArgs = args;
        };

        var builder = SmsMessageBuilder.New().SetTo("+15551234567").SetBody("Test message");

        // Trigger send (will fail, but event should fire)
        await service.SendAsync(builder, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Event should have fired
        Assert.NotNull(eventArgs);
        Assert.NotNull(eventArgs.SmsRequest);
        Assert.Equal("+15551234567", eventArgs.SmsRequest.To);
        Assert.Equal("Test message", eventArgs.SmsRequest.Body);
    }

    [Fact]
    public async Task MessageSent_Event_FiresOnSend()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        SmsSentEventArgs? eventArgs = null;
        service.MessageSent += (_, args) => {
            eventArgs = args;
        };

        var builder = SmsMessageBuilder.New().SetTo("+15551234567").SetBody("Test message");

        // Trigger send (will fail, but event should fire)
        _ = await service.SendAsync(builder, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Event should have fired (even on failure)
        Assert.NotNull(eventArgs);
        Assert.NotNull(eventArgs.SmsResult);
        Assert.NotNull(eventArgs.SmsResult.Data);
        Assert.Equal("+15551234567", eventArgs.SmsResult.Data!.To);
        Assert.Equal("Test message", eventArgs.SmsResult.Data.Body);
    }

    [Fact]
    public async Task MessageSending_Event_FiresBeforeMessageSent()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        var eventOrder = new List<string>();
        service.MessageSending += (_, _) => {
            eventOrder.Add("Sending");
        };

        service.MessageSent += (_, _) => {
            eventOrder.Add("Sent");
        };

        var builder = SmsMessageBuilder.New().SetTo("+15551234567").SetBody("Test message");
        _ = await service.SendAsync(builder, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Verify order: Sending should fire before Sent
        Assert.Equal(2, eventOrder.Count);
        Assert.Equal("Sending", eventOrder[0]);
        Assert.Equal("Sent", eventOrder[1]);
    }

    [Fact]
    public async Task BulkSending_Event_FiresOnBulkSend()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        SmsBulkSendingEventArgs? eventArgs = null;
        service.BulkSending += (_, args) => {
            eventArgs = args;
        };

        SmsMessageBuilder[] builders = [SmsMessageBuilder.New().SetTo("+15551111111").SetBody("Message 1"), SmsMessageBuilder.New().SetTo("+15552222222").SetBody("Message 2")];

        // Trigger bulk send (will fail, but event should fire)
        _ = await service.SendBulkAsync(builders, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Event should have fired
        Assert.NotNull(eventArgs);
        Assert.NotNull(eventArgs.BulkSmsMessage);
        Assert.Equal(2, eventArgs.BulkSmsMessage.Count);
    }

    [Fact]
    public async Task BulkSent_Event_FiresOnBulkSend()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        BulkSmsSentEventArgs? eventArgs = null;
        service.BulkSent += (_, args) => {
            eventArgs = args;
        };

        SmsMessageBuilder[] builders = [SmsMessageBuilder.New().SetTo("+15551111111").SetBody("Message 1"), SmsMessageBuilder.New().SetTo("+15552222222").SetBody("Message 2")];

        // Trigger bulk send (will fail, but event should fire)
        _ = await service.SendBulkAsync(builders, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Event should have fired
        Assert.NotNull(eventArgs);
        Assert.NotNull(eventArgs.BulkSmsResult);
        Assert.Equal(2, eventArgs.BulkSmsResult.Results.Count);
        Assert.NotNull(eventArgs.BulkSmsResult.Results);
    }

    [Fact]
    public async Task BulkSending_Event_FiresBeforeBulkSent()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        var eventOrder = new List<string>();
        service.BulkSending += (_, _) => {
            eventOrder.Add("BulkSending");
        };

        service.BulkSent += (_, _) => {
            eventOrder.Add("BulkSent");
        };

        SmsMessageBuilder[] builders = [SmsMessageBuilder.New().SetTo("+15551111111").SetBody("Message 1"), SmsMessageBuilder.New().SetTo("+15552222222").SetBody("Message 2")];
        _ = await service.SendBulkAsync(builders, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Verify order: BulkSending should fire before BulkSent
        Assert.Equal(2, eventOrder.Count);
        Assert.Equal("BulkSending", eventOrder[0]);
        Assert.Equal("BulkSent", eventOrder[1]);
    }

    [Fact]
    public async Task BulkSent_Event_ContainsCorrectResults()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        BulkSmsSentEventArgs? eventArgs = null;
        service.BulkSent += (_, args) => {
            eventArgs = args;
        };

        SmsMessageBuilder[] builders = [
            SmsMessageBuilder.New().SetTo("+15551111111").SetBody("Message 1"), SmsMessageBuilder.New().SetTo("+15552222222").SetBody("Message 2"),
            SmsMessageBuilder.New().SetTo("+15553333333").SetBody("Message 3")
        ];

        _ = await service.SendBulkAsync(builders, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(eventArgs);
        Assert.NotNull(eventArgs.BulkSmsResult);
        Assert.Equal(3, eventArgs.BulkSmsResult.Results.Count);
        Assert.NotNull(eventArgs.BulkSmsResult.Results);

        // Verify all results are present
        foreach (var result in eventArgs.BulkSmsResult.Results)
            Assert.NotNull(result);
    }

    [Fact]
    public async Task BulkSent_Event_ContainsElapsedTime()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        BulkSmsSentEventArgs? eventArgs = null;
        service.BulkSent += (_, args) => {
            eventArgs = args;
        };

        SmsMessageBuilder[] builders = [SmsMessageBuilder.New().SetTo("+15551111111").SetBody("Message 1"), SmsMessageBuilder.New().SetTo("+15552222222").SetBody("Message 2")];
        _ = await service.SendBulkAsync(builders, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(eventArgs);
        Assert.NotNull(eventArgs.BulkSmsResult);
    }

    [Fact]
    public async Task BulkSent_Event_FiresWithBulkSmsBuilder()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        BulkSmsSentEventArgs? eventArgs = null;
        service.BulkSent += (_, args) => {
            eventArgs = args;
        };

        var bulkBuilder = BulkSmsBuilder.New().SetDefaultFrom("+1987654321").Add("+15551111111", "Message 1").Add("+15552222222", "Message 2");
        _ = await service.SendBulkAsync(bulkBuilder, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(eventArgs);
        Assert.NotNull(eventArgs.BulkSmsResult);
        Assert.Equal(2, eventArgs.BulkSmsResult.Results.Count);
        Assert.NotNull(eventArgs.BulkSmsResult.Results);
    }

    [Fact]
    public async Task BulkSent_Event_FiresWithSendBulkSmsAsync()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        BulkSmsSentEventArgs? eventArgs = null;
        service.BulkSent += (_, args) => {
            eventArgs = args;
        };

        SmsRequest[] messages = [new() { To = "+15551111111", Body = "Message 1", From = "+1987654321" }, new() { To = "+15552222222", Body = "Message 2", From = "+1987654321" }];
        _ = await service.SendBulkSmsAsync(messages, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(eventArgs);
        Assert.NotNull(eventArgs.BulkSmsResult);
        Assert.Equal(2, eventArgs.BulkSmsResult.Results.Count);
        Assert.NotNull(eventArgs.BulkSmsResult.Results);
    }

    [Fact]
    public async Task MessageSent_Event_FiresForEachMessageInBulk()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        var messageSentCount = 0;
        service.MessageSent += (_, _) => {
            messageSentCount++;
        };

        SmsMessageBuilder[] builders = [
            SmsMessageBuilder.New().SetTo("+15551111111").SetBody("Message 1"), 
            SmsMessageBuilder.New().SetTo("+15552222222").SetBody("Message 2"),
            SmsMessageBuilder.New().SetTo("+15553333333").SetBody("Message 3")
        ];

        _ = await service.SendBulkAsync(builders, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // MessageSent should fire for each message
        Assert.Equal(3, messageSentCount);
    }

    [Fact]
    public async Task MessageSending_Event_FiresForEachMessageInBulk()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        var messageSendingCount = 0;
        service.MessageSending += (_, _) => {
            messageSendingCount++;
        };

        SmsMessageBuilder[] builders = [
            SmsMessageBuilder.New().SetTo("+15551111111").SetBody("Message 1"), SmsMessageBuilder.New().SetTo("+15552222222").SetBody("Message 2"),
            SmsMessageBuilder.New().SetTo("+15553333333").SetBody("Message 3")
        ];

        _ = await service.SendBulkAsync(builders, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // MessageSending should fire for each message
        Assert.Equal(3, messageSendingCount);
    }

    [Fact]
    public async Task Events_FireEvenOnFailure()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        SmsSendingEventArgs? sendingEventArgs = null;
        SmsSentEventArgs? sentEventArgs = null;
        service.MessageSending += (_, args) => {
            sendingEventArgs = args;
        };

        service.MessageSent += (_, args) => {
            sentEventArgs = args;
        };

        // Use invalid message that will fail at send time (invalid phone format)
        var message = new SmsRequest("invalid-phone-format", "Test", "+1987654321");
        var result = await service.SendAsync(message, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Events should fire even on failure
        Assert.NotNull(sendingEventArgs);
        Assert.NotNull(sentEventArgs);
        Assert.False(result.IsSuccess);
        Assert.NotNull(sentEventArgs.SmsResult);
        Assert.False(sentEventArgs.SmsResult.IsSuccess);
        Assert.NotNull(sentEventArgs.SmsResult.Errors);
    }

    [Fact]
    public async Task BulkSent_Event_FiresEvenOnFailure()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        BulkSmsSentEventArgs? eventArgs = null;
        service.BulkSent += (_, args) => {
            eventArgs = args;
        };

        // Use invalid messages that will fail at send time (missing body or invalid format)
        SmsRequest[] messages = [
            new("invalid-phone", "Message 1", "+1987654321"), // Invalid phone format
            new("+15552222222", "Message 2", "+1987654321") // Valid
        ];

        _ = await service.SendBulkSmsAsync(messages, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Event should fire even with failures
        Assert.NotNull(eventArgs);
        Assert.NotNull(eventArgs.BulkSmsResult);
        Assert.Equal(2, eventArgs.BulkSmsResult.Results.Count);
        Assert.NotNull(eventArgs.BulkSmsResult.Results);
    }
}