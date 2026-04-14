using Lyo.Email.Models;
using Lyo.Metrics;
using Lyo.Testing;
using Microsoft.Extensions.Logging;

namespace Lyo.Email.Tests;

public class EmailServiceConstructorTests
{
    private readonly ILogger<EmailService> _logger;

    private readonly EmailServiceOptions _validOptions = new() {
        Host = "smtp.example.com",
        Port = 587,
        UseSsl = true,
        DefaultFromAddress = "test@example.com",
        DefaultFromName = "Test Sender",
        Username = "testuser",
        Password = "testpass"
    };

    public EmailServiceConstructorTests(ITestOutputHelper output)
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
        var service = new EmailService(_validOptions, _logger);
        service.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_ValidOptionsWithNullLogger_CreatesService()
    {
        var service = new EmailService(_validOptions);
        service.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_ValidOptionsWithNullMetrics_CreatesService()
    {
        var service = new EmailService(_validOptions, _logger);
        service.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_ValidOptionsWithMetrics_CreatesService()
    {
        var metrics = NullMetrics.Instance;
        var service = new EmailService(_validOptions, _logger, metrics);
        service.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_ValidOptionsWithMetricsDisabled_CreatesService()
    {
        var options = new EmailServiceOptions {
            Host = "smtp.example.com",
            Port = 587,
            UseSsl = true,
            DefaultFromAddress = "test@example.com",
            DefaultFromName = "Test Sender",
            EnableMetrics = false
        };

        var metrics = NullMetrics.Instance;
        var service = new EmailService(options, _logger, metrics);
        service.ShouldNotBeNull();
    }
}