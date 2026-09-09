using Lyo.Common.Metadata.Records;
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
        Port = PortInfo.SmtpSubmission,
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
    public void Constructor_ValidOptionsNullLogger_CreatesService()
    {
        var service = new EmailService(_options);
        service.ShouldNotBeNull();
    }

    [Fact]
    public async Task SendEmailAsync_BuilderWithFromAddress_UsesProvidedFrom()
    {
        var service = new EmailService(_options, _logger);
        var builder = EmailRequestBuilder.New().SetSubject("Test Subject").SetTextBody("Test Body").SetFrom("custom@example.com", "Custom Sender").AddTo("recipient@example.com");

        // Invalid SMTP fails at runtime; this still checks the API shape
        var result = await service.SendEmailAsync(builder, TestContext.Current.CancellationToken);
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

        // Invalid SMTP fails at runtime; this still checks the API shape
        var result = await service.SendEmailAsync(builder, TestContext.Current.CancellationToken);
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
        var result = await service.SendEmailAsync(builder, "override@example.com", "Override", TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Data.ShouldNotBeNull();
        // The override must win over the builder From
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

        var results = await service.SendBulkEmailAsync(builders, TestContext.Current.CancellationToken);
        results.ShouldHaveCount(2);
    }

    [Fact]
    public async Task SendBulkEmailAsync_SingleBuilder_ProcessesOne()
    {
        var service = new EmailService(_options, _logger);
        EmailRequestBuilder[] builders = [EmailRequestBuilder.New().SetSubject("Test").SetTextBody("Body").AddTo("test@example.com")];
        var results = await service.SendBulkEmailAsync(builders, TestContext.Current.CancellationToken);
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

        // Start a send (it fails; the event should still fire)
        _ = service.SendEmailAsync(builder, TestContext.Current.CancellationToken).ContinueWith(_ => { }, TestContext.Current.CancellationToken);

        // Brief wait
        await Task.Delay(100, TestContext.Current.CancellationToken);

        // The event should fire even when the send fails
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

        // Start a bulk send (it fails; the event should still fire)
        _ = service.SendBulkEmailAsync(builders, TestContext.Current.CancellationToken).ContinueWith(_ => { }, TestContext.Current.CancellationToken);

        // Brief wait
        await Task.Delay(200, TestContext.Current.CancellationToken);

        // The event should have been raised
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

        // Start a connection test (it fails; the event should still fire)
        _ = service.TestConnectionAsync(TestContext.Current.CancellationToken).ContinueWith(_ => { }, TestContext.Current.CancellationToken);

        // Brief wait
        await Task.Delay(200, TestContext.Current.CancellationToken);

        // The event should have been raised
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

        var result = await service.SendEmailAsync(builder, TestContext.Current.CancellationToken);
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
        result.Data.TextBody.ShouldBe("Test Body");
    }

    [Fact]
    public async Task SendEmailAsync_BuilderWithHtmlAndText_PutsBodiesOnRequest()
    {
        var service = new EmailService(_options, _logger);
        var builder = EmailRequestBuilder.New()
            .SetSubject("HTML Email")
            .SetHtmlBody("<p>Hello</p>")
            .SetTextBody("Hello")
            .AddTo("recipient@example.com");

        var result = await service.SendEmailAsync(builder, TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Data.ShouldNotBeNull();
        result.Data!.TextBody.ShouldBe("Hello");
        result.Data.HtmlBody.ShouldBe("<p>Hello</p>");
    }

    [Fact]
    public async Task SendEmailAsync_EmailRequestWithBodies_KeepsBodiesOnResult()
    {
        var service = new EmailService(_options, _logger);
        var request = new EmailRequest(
            FromAddress: "test@example.com",
            ToAddresses: ["recipient@example.com"],
            Subject: "Request body",
            TextBody: "Plain from request",
            HtmlBody: "<p>Html from request</p>");

        var result = await service.SendEmailAsync(request, TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Data.ShouldNotBeNull();
        result.Data!.TextBody.ShouldBe("Plain from request");
        result.Data.HtmlBody.ShouldBe("<p>Html from request</p>");
    }

    [Fact]
    public void BuildMimeMessageFromRequest_WithBodies_AppliesHtmlAndText()
    {
        var service = new EmailService(_options, _logger);
        var request = new EmailRequest(
            FromAddress: "test@example.com",
            ToAddresses: ["recipient@example.com"],
            Subject: "Bodies",
            TextBody: "Plain",
            HtmlBody: "<p>Hi</p>");

        var mime = service.BuildMimeMessageFromRequest(request);
        mime.TextBody.ShouldBe("Plain");
        mime.HtmlBody.ShouldBe("<p>Hi</p>");
        mime.Subject.ShouldBe("Bodies");
    }

    [Fact]
    public async Task SendEmailAsync_ResultContainsElapsedTime()
    {
        var service = new EmailService(_options, _logger);
        var builder = EmailRequestBuilder.New().SetSubject("Test").SetTextBody("Body").AddTo("test@example.com");
        var result = await service.SendEmailAsync(builder, TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Timestamp.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

    [Fact]
    public async Task SendEmailAsync_SuccessResult_HasSuccessProperties()
    {
        // A successful send would populate these properties
        // Invalid SMTP fails here; we still check the result shape
        var service = new EmailService(_options, _logger);
        var builder = EmailRequestBuilder.New().SetSubject("Test").SetTextBody("Body").AddTo("test@example.com");
        var result = await service.SendEmailAsync(builder, TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        // Invalid SMTP yields failure; the result shape is still what we assert
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