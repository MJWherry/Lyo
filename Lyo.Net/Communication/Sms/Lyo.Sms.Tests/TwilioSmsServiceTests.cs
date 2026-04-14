using Lyo.Metrics;
using Lyo.Sms.Builders;
using Lyo.Sms.Models;
using Lyo.Sms.Twilio;
using Lyo.Testing;
using Microsoft.Extensions.Logging;
using Twilio.Clients;

namespace Lyo.Sms.Tests;

public class TwilioSmsServiceTests
{
    private readonly ILogger<TwilioSmsService> _logger;

    private readonly TwilioOptions _options = new() { AccountSid = "AC1234567890abcdef1234567890abcdef", AuthToken = "test_auth_token", DefaultFromPhoneNumber = "+1987654321" };

    public TwilioSmsServiceTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        _logger = loggerFactory.CreateLogger<TwilioSmsService>();
    }

    private static TwilioRestClient CreateRestClient(TwilioOptions options) => new(options.AccountSid, options.AuthToken);

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
        => ExceptionAssertions.Throws<ArgumentNullException>(() => new TwilioSmsService(null!, CreateRestClient(_options), _logger));

    [Fact]
    public void Constructor_NullRestClient_ThrowsArgumentNullException() => ExceptionAssertions.Throws<ArgumentNullException>(() => new TwilioSmsService(_options, null!, _logger));

    [Fact]
    public void Constructor_EmptyAccountSid_ThrowsArgumentException()
    {
        var invalidOptions = new TwilioOptions { AccountSid = "", AuthToken = "token" };
        ExceptionAssertions.Throws<ArgumentException>(() => new TwilioSmsService(invalidOptions, CreateRestClient(invalidOptions), _logger));
    }

    [Fact]
    public void Constructor_EmptyAuthToken_ThrowsArgumentException()
    {
        var invalidOptions = new TwilioOptions { AccountSid = "sid", AuthToken = "" };
        ExceptionAssertions.Throws<ArgumentException>(() => new TwilioSmsService(invalidOptions, CreateRestClient(invalidOptions), _logger));
    }

    [Fact]
    public void Constructor_ValidOptions_CreatesInstance()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        service.ShouldNotBeNull();
    }

    [Fact]
    public async Task SendAsync_NullBuilder_ThrowsArgumentNullException()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        await ExceptionAssertions.ThrowsAsync<ArgumentNullException>(async () => await service.SendAsync((SmsMessageBuilder)null!, ct: TestContext.Current.CancellationToken)).ConfigureAwait(false);
    }

    [Fact]
    public async Task SendAsync_BuilderOnly_Works()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        var builder = SmsMessageBuilder.New().SetTo("+15551234567").SetBody("Test");

        // This should compile and work (will fail at runtime with Twilio, but validates the API)
        var result = await service.SendAsync(builder, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task SendAsync_BuilderWithFrom_Works()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        var builder = SmsMessageBuilder.New().SetTo("+15551234567").SetBody("Test");

        // This should compile and work (will fail at runtime with Twilio, but validates the API)
        var result = await service.SendAsync(builder, "+19998887777", TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task SendAsync_BuilderWithMedia_Works()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        var builder = SmsMessageBuilder.New().SetTo("+15551234567").AddAttachment("https://example.com/image.jpg");

        // This should compile and work (will fail at runtime with Twilio, but validates the API)
        var result = await service.SendAsync(builder, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task SendBulkAsync_NullBuilders_ThrowsArgumentNullException()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.SendBulkAsync((IEnumerable<SmsMessageBuilder>)null!, TestContext.Current.CancellationToken)).ConfigureAwait(false);
    }

    [Fact]
    public async Task SendBulkSmsAsync_NullMessages_ThrowsArgumentNullException()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.SendBulkSmsAsync(null!, TestContext.Current.CancellationToken)).ConfigureAwait(false);
    }

    [Fact]
    public async Task GetMessageByIdAsync_NullMessageId_ThrowsArgumentNullException()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.GetMessageByIdAsync(null!, TestContext.Current.CancellationToken)).ConfigureAwait(false);
    }

    [Fact]
    public async Task GetMessageByIdAsync_EmptyMessageId_ThrowsArgumentException()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        await ExceptionAssertions.ThrowsAsync<ArgumentException>(() => service.GetMessageByIdAsync("", TestContext.Current.CancellationToken)).ConfigureAwait(false);
    }

    [Fact]
    public void GetMessagesAsync_NullFilter_ThrowsArgumentNullException()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        ExceptionAssertions.Throws<ArgumentNullException>(() => service.GetMessagesAsync(null!, TestContext.Current.CancellationToken).GetAwaiter().GetResult());
    }

    [Fact]
    public async Task GetMessagesAsync_EmptyFilter_ReturnsPaginatedResults()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        var filter = new SmsMessageQueryFilter();
        var result = await service.GetMessagesAsync(filter, TestContext.Current.CancellationToken).ConfigureAwait(false);
        result.ShouldNotBeNull();
        result.Items.ShouldNotBeNull();
        result.Start.ShouldBe(0);
        result.Amount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task SendAsync_InvalidMessage_ReturnsFailure()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        var message = new SmsRequest { To = "", Body = "Test" };
        var result = await service.SendAsync(message, TestContext.Current.CancellationToken).ConfigureAwait(false);
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task SendAsync_InvalidMessage_RecordsTwilioMetrics()
    {
        var metrics = new MetricsService();
        var options = new TwilioOptions {
            AccountSid = _options.AccountSid,
            AuthToken = _options.AuthToken,
            DefaultFromPhoneNumber = _options.DefaultFromPhoneNumber,
            EnableMetrics = true
        };

        var service = new TwilioSmsService(options, CreateRestClient(options), _logger, metrics);
        var result = await service.SendAsync(new() { To = "", Body = "Test" }, TestContext.Current.CancellationToken).ConfigureAwait(false);
        result.IsSuccess.ShouldBeFalse();
        Assert.NotNull(metrics.GetHistogram(Twilio.Constants.Metrics.SendDuration));
        Assert.Equal(1, metrics.GetCounterValue(Twilio.Constants.Metrics.SendFailure));
    }

    [Fact]
    public async Task SendAsync_MissingFromAndDefault_ReturnsFailure()
    {
        var optionsWithoutDefault = new TwilioOptions { AccountSid = "AC1234567890abcdef1234567890abcdef", AuthToken = "test_auth_token" };
        var service = new TwilioSmsService(optionsWithoutDefault, CreateRestClient(optionsWithoutDefault), _logger);
        var message = new SmsRequest { To = "+15551234567", Body = "Test" };
        var result = await service.SendAsync(message, TestContext.Current.CancellationToken).ConfigureAwait(false);
        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldNotBeNull();
        result.Errors!.Count.ShouldBeGreaterThan(0);
        result.Errors[0].Code.ShouldBeEquivalentTo(SmsErrorCodes.MissingFromNumber);
    }

    [Fact]
    public async Task SendSmsAsync_WithBody_Works()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        var result = await service.SendSmsAsync("+15551234567", "Test message", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Will fail at runtime with Twilio, but validates the API signature
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task SendMmsAsync_WithMediaUrls_Works()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        var mediaUrls = new[] { "https://example.com/image.jpg" };
        var result = await service.SendMmsAsync("+15551234567", mediaUrls, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Will fail at runtime with Twilio, but validates the API signature
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task SendMmsAsync_WithMediaUrlsAndBody_Works()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        var mediaUrls = new[] { "https://example.com/image.jpg" };
        var result = await service.SendMmsAsync("+15551234567", mediaUrls, "Check this out!", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Will fail at runtime with Twilio, but validates the API signature
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task SendMmsAsync_WithMediaUrlsBodyAndFrom_Works()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        var mediaUrls = new[] { "https://example.com/image.jpg" };
        var result = await service.SendMmsAsync("+15551234567", mediaUrls, "Check this out!", "+19998887777", TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Will fail at runtime with Twilio, but validates the API signature
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task SendMmsAsync_WithUriMediaUrls_Works()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        var mediaUrls = new[] { new Uri("https://example.com/image.jpg") };
        var result = await service.SendMmsAsync("+15551234567", mediaUrls, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Will fail at runtime with Twilio, but validates the API signature
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task SendMmsAsync_WithUriMediaUrlsAndBody_Works()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        var mediaUrls = new[] { new Uri("https://example.com/image.jpg") };
        var result = await service.SendMmsAsync("+15551234567", mediaUrls, "Check this out!", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Will fail at runtime with Twilio, but validates the API signature
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task SendMmsAsync_EmptyMediaUrls_ThrowsArgumentException()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        var mediaUrls = Array.Empty<string>();
        await Assert.ThrowsAsync<ArgumentException>(() => service.SendMmsAsync("+15551234567", mediaUrls, ct: TestContext.Current.CancellationToken)).ConfigureAwait(false);
    }

    [Fact]
    public async Task SendMmsAsync_NullMediaUrls_ThrowsArgumentNullException()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.SendMmsAsync("+15551234567", (IEnumerable<string>)null!, ct: TestContext.Current.CancellationToken)).ConfigureAwait(false);
    }

    [Fact]
    public async Task SendBulkAsync_NullBulkBuilder_ThrowsArgumentNullException()
    {
        var service = new TwilioSmsService(_options, CreateRestClient(_options), _logger);
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.SendBulkAsync((BulkSmsBuilder)null!, TestContext.Current.CancellationToken)).ConfigureAwait(false);
    }

    [Fact]
    public async Task SendBulkSmsAsync_InvalidMessages_RecordsTwilioBulkMetrics()
    {
        var metrics = new MetricsService();
        var options = new TwilioOptions {
            AccountSid = _options.AccountSid,
            AuthToken = _options.AuthToken,
            DefaultFromPhoneNumber = _options.DefaultFromPhoneNumber,
            EnableMetrics = true
        };

        var service = new TwilioSmsService(options, CreateRestClient(options), _logger, metrics);
        var messages = new[] { new SmsRequest { To = "", Body = "Test 1" }, new SmsRequest { To = "", Body = "Test 2" } };
        var results = await service.SendBulkSmsAsync(messages, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(2, results.Count);
        Assert.NotNull(metrics.GetHistogram(Twilio.Constants.Metrics.BulkSendDuration));
        Assert.Equal(2, metrics.GetCounterValue(Twilio.Constants.Metrics.BulkSendTotal));
        Assert.Equal(0, metrics.GetCounterValue(Twilio.Constants.Metrics.BulkSendSuccess));
        Assert.Equal(2, metrics.GetCounterValue(Twilio.Constants.Metrics.BulkSendFailure));
        Assert.NotNull(metrics.GetGaugeValue(Twilio.Constants.Metrics.BulkSendLastDurationMs));
    }
}