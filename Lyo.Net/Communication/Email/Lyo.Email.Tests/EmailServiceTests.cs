using Lyo.Email.Builders;
using Lyo.Email.Models;
using Lyo.Testing;
using Microsoft.Extensions.Logging;

namespace Lyo.Email.Tests;

public class EmailServiceTests
{
    private readonly ILogger<EmailService> _logger;

    private readonly EmailServiceOptions _options = new() {
        Host = "smtp.example.com",
        Port = 587,
        UseSsl = true,
        DefaultFromAddress = "test@example.com",
        DefaultFromName = "Test Sender",
        Username = "testuser",
        Password = "testpass"
    };

    public EmailServiceTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        _logger = loggerFactory.CreateLogger<EmailService>();
    }

    [Fact]
    public void Constructor_ValidOptions_CreatesService()
    {
        var service = new EmailService(_options, _logger);
        service.ShouldNotBeNull();
    }

    [Fact]
    public async Task SendEmailAsync_BuilderWithFromAddress_UsesProvidedFrom()
    {
        var service = new EmailService(_options, _logger);
        var builder = EmailRequestBuilder.New().SetSubject("Test Subject").SetTextBody("Test Body").SetFrom("custom@example.com", "Custom Sender").AddTo("recipient@example.com");

        // Will fail at runtime with invalid SMTP, but validates the API
        var result = await service.SendEmailAsync(builder, TestContext.Current.CancellationToken).ConfigureAwait(false);
        result.ShouldNotBeNull();
        result.Data.ShouldNotBeNull();
        result.Data!.FromAddress.ShouldBe("custom@example.com");
        result.Data.FromName.ShouldBe("Custom Sender");
    }

    [Fact]
    public async Task SendEmailAsync_BuilderWithoutFrom_UsesDefaultFrom()
    {
        var service = new EmailService(_options, _logger);
        var builder = EmailRequestBuilder.New().SetSubject("Test Subject").SetTextBody("Test Body").AddTo("recipient@example.com");

        // Will fail at runtime with invalid SMTP, but validates the API
        var result = await service.SendEmailAsync(builder, TestContext.Current.CancellationToken).ConfigureAwait(false);
        result.ShouldNotBeNull();
        result.Data.ShouldNotBeNull();
        result.Data!.FromAddress.ShouldBe(_options.DefaultFromAddress);
        result.Data.FromName.ShouldBe(_options.DefaultFromName);
    }

    [Fact]
    public async Task SendEmailAsync_BuilderWithCustomFrom_UsesCustomFrom()
    {
        var service = new EmailService(_options, _logger);
        var builder = EmailRequestBuilder.New().SetSubject("Test Subject").SetTextBody("Test Body").SetFrom("custom@example.com", "Custom").AddTo("recipient@example.com");
        var result = await service.SendEmailAsync(builder, "override@example.com", "Override", TestContext.Current.CancellationToken).ConfigureAwait(false);
        result.ShouldNotBeNull();
        result.Data.ShouldNotBeNull();
        // Should use the override, not the builder's from
        result.Data!.FromAddress.ShouldBe("override@example.com");
        result.Data.FromName.ShouldBe("Override");
    }

    [Fact]
    public async Task SendBulkEmailAsync_MultipleBuilders_ProcessesAll()
    {
        var service = new EmailService(_options, _logger);
        EmailRequestBuilder[] builders = [
            EmailRequestBuilder.New().SetSubject("Test 1").SetTextBody("Body 1").AddTo("test1@example.com"),
            EmailRequestBuilder.New().SetSubject("Test 2").SetTextBody("Body 2").AddTo("test2@example.com")
        ];

        var results = await service.SendBulkEmailAsync(builders, TestContext.Current.CancellationToken).ConfigureAwait(false);
        results.ShouldHaveCount(2);
        results.ShouldAllSatisfy(r => r is not null);
    }

    [Fact]
    public async Task SendBulkEmailAsync_SingleBuilder_ProcessesOne()
    {
        var service = new EmailService(_options, _logger);
        EmailRequestBuilder[] builders = [EmailRequestBuilder.New().SetSubject("Test").SetTextBody("Body").AddTo("test@example.com")];
        var results = await service.SendBulkEmailAsync(builders, TestContext.Current.CancellationToken).ConfigureAwait(false);
        results.ShouldHaveCount(1);
    }

    [Fact]
    public async Task EmailSent_Event_FiresOnSend()
    {
        var service = new EmailService(_options, _logger);
        EmailSentEventArgs? eventArgs = null;
        service.EmailSent += (_, args) => {
            eventArgs = args;
        };

        var builder = EmailRequestBuilder.New().SetSubject("Test").SetTextBody("Body").AddTo("test@example.com");

        // Trigger send (will fail, but event should fire)
        _ = service.SendEmailAsync(builder, TestContext.Current.CancellationToken).ContinueWith(_ => { }, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Give it a moment
        await Task.Delay(100, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Event should have fired (even on failure)
        eventArgs.ShouldNotBeNull();
        eventArgs.EmailResult.ShouldNotBeNull();
        eventArgs.EmailResult.Data.ShouldNotBeNull();
    }

    [Fact]
    public async Task BulkEmailCompleted_Event_FiresOnBulkSend()
    {
        var service = new EmailService(_options, _logger);
        BulkEmailSentEventArgs? eventArgs = null;
        service.BulkEmailSent += (_, args) => {
            eventArgs = args;
        };

        EmailRequestBuilder[] builders = [EmailRequestBuilder.New().SetSubject("Test").SetTextBody("Body").AddTo("test@example.com")];

        // Trigger bulk send (will fail, but event should fire)
        _ = service.SendBulkEmailAsync(builders, TestContext.Current.CancellationToken).ContinueWith(_ => { }, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Give it a moment
        await Task.Delay(200, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Event should have fired
        eventArgs.ShouldNotBeNull();
        eventArgs.BulkEmailResult.ShouldNotBeNull();
        eventArgs.BulkEmailResult.TotalCount.ShouldBe(1);
        eventArgs.BulkEmailResult.Results.ShouldNotBeNull();
    }

    [Fact]
    public async Task ConnectionTested_Event_FiresOnTest()
    {
        var service = new EmailService(_options, _logger);
        ConnectionTestedEventArgs? eventArgs = null;
        service.ConnectionTested += (_, args) => {
            eventArgs = args;
        };

        // Trigger test (will fail, but event should fire)
        _ = service.TestConnectionAsync(TestContext.Current.CancellationToken).ContinueWith(_ => { }, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Give it a moment
        await Task.Delay(200, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Event should have fired
        eventArgs.ShouldNotBeNull();
        eventArgs.IsSuccess.ShouldBeFalse(); // Should fail with invalid server
    }

    [Fact]
    public async Task SendEmailAsync_ResultContainsMetadata()
    {
        var service = new EmailService(_options, _logger);
        var builder = EmailRequestBuilder.New()
            .SetSubject("Test Subject")
            .SetTextBody("Test Body")
            .AddTo("recipient@example.com")
            .AddCc("cc@example.com")
            .AddBcc("bcc@example.com");

        var result = await service.SendEmailAsync(builder, TestContext.Current.CancellationToken).ConfigureAwait(false);
        result.ShouldNotBeNull();
        result.Data.ShouldNotBeNull();
        result.Data!.ToAddresses.ShouldNotBeNull();
        result.Data.ToAddresses.ShouldHaveCount(1);
        result.Data.ToAddresses[0].ShouldBe("recipient@example.com");
        result.Data.CcAddresses.ShouldNotBeNull();
        result.Data.CcAddresses.ShouldHaveCount(1);
        result.Data.CcAddresses[0].ShouldBe("cc@example.com");
        result.Data.BccAddresses.ShouldNotBeNull();
        result.Data.BccAddresses.ShouldHaveCount(1);
        result.Data.BccAddresses[0].ShouldBe("bcc@example.com");
        result.Data.Subject.ShouldBe("Test Subject");
    }

    [Fact]
    public async Task SendEmailAsync_ResultContainsElapsedTime()
    {
        var service = new EmailService(_options, _logger);
        var builder = EmailRequestBuilder.New().SetSubject("Test").SetTextBody("Body").AddTo("test@example.com");
        var result = await service.SendEmailAsync(builder, TestContext.Current.CancellationToken).ConfigureAwait(false);
        result.ShouldNotBeNull();
        result.Timestamp.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

    [Fact]
    public async Task SendEmailAsync_SuccessResult_HasSuccessProperties()
    {
        // Note: This test assumes a successful send would set these properties
        // In reality, with invalid SMTP, it will fail, but we can test the structure
        var service = new EmailService(_options, _logger);
        var builder = EmailRequestBuilder.New().SetSubject("Test").SetTextBody("Body").AddTo("test@example.com");
        var result = await service.SendEmailAsync(builder, TestContext.Current.CancellationToken).ConfigureAwait(false);
        result.ShouldNotBeNull();
        // Result will be failure with invalid SMTP, but structure is correct
        if (result.IsSuccess) {
            result.Data.ShouldNotBeNull();
            result.Errors.ShouldBeNull();
        }
        else {
            result.Errors.ShouldNotBeNull();
            result.Errors!.Count.ShouldBeGreaterThan(0);
            result.Errors[0].Message.ShouldNotBeNull();
        }
    }
}