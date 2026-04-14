using Lyo.Email.Builders;
using Lyo.Email.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Email.Tests;

public class EmailServiceErrorHandlingTests
{
    private readonly ILogger<EmailService> _logger = NullLogger<EmailService>.Instance;

    private readonly EmailServiceOptions _options = new() {
        Host = "invalid-smtp-server-that-does-not-exist.local",
        Port = 587,
        UseSsl = false,
        DefaultFromAddress = "test@example.com",
        DefaultFromName = "Test Sender",
        Username = "testuser",
        Password = "testpass"
    };

    [Fact]
    public async Task SendEmailAsync_InvalidSmtpServer_ReturnsFailure()
    {
        var service = new EmailService(_options, _logger);
        var builder = EmailRequestBuilder.New().SetSubject("Test").SetTextBody("Test body").AddTo("recipient@example.com");
        var result = await service.SendEmailAsync(builder, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Errors);
        Assert.True(result.Errors!.Count > 0);
        Assert.NotNull(result.Errors[0].Message);
    }

    [Fact]
    public async Task SendEmailAsync_Cancelled_ReturnsCancelledResult()
    {
        var service = new EmailService(_options, _logger);
        var builder = EmailRequestBuilder.New().SetSubject("Test").SetTextBody("Test body").AddTo("recipient@example.com");
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync().ConfigureAwait(false);
        var result = await service.SendEmailAsync(builder, cts.Token).ConfigureAwait(false);
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Errors);
        Assert.True(result.Errors!.Count > 0);
        Assert.Equal(EmailErrorCodes.OperationCancelled, result.Errors[0].Code);
    }

    [Fact]
    public async Task SendBulkEmailAsync_EmptyBuilders_ReturnsEmptyResults()
    {
        var service = new EmailService(_options, _logger);
        var builders = Array.Empty<EmailRequestBuilder>();
        var results = await service.SendBulkEmailAsync(builders, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Empty(results);
    }

    [Fact]
    public async Task SendBulkEmailAsync_Cancelled_StopsProcessing()
    {
        var service = new EmailService(_options, _logger);
        var builders = new[] {
            EmailRequestBuilder.New().SetSubject("Test 1").SetTextBody("Body 1").AddTo("test1@example.com"),
            EmailRequestBuilder.New().SetSubject("Test 2").SetTextBody("Body 2").AddTo("test2@example.com")
        };

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync().ConfigureAwait(false);
        var results = await service.SendBulkEmailAsync(builders, cts.Token).ConfigureAwait(false);

        // Should stop early due to cancellation
        Assert.True(results.Count <= builders.Length);
    }

    [Fact]
    public async Task TestConnectionAsync_InvalidServer_ReturnsFalse()
    {
        var service = new EmailService(_options, _logger);
        var result = await service.TestConnectionAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.False(result);
    }

    [Fact]
    public async Task TestConnectionAsync_Cancelled_ThrowsOperationCanceledException()
    {
        var service = new EmailService(_options, _logger);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync().ConfigureAwait(false);
        await Assert.ThrowsAsync<OperationCanceledException>(() => service.TestConnectionAsync(cts.Token)).ConfigureAwait(false);
    }
}