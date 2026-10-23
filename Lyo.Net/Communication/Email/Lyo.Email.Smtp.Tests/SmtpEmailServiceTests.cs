using Lyo.Email.Builders;
using Lyo.Email.Models;
using Lyo.Exceptions.Models;

namespace Lyo.Email.Smtp.Tests;

public class SmtpEmailServiceTests
{
    [Fact]
    public async Task SendEmailAsync_UnreachableHost_ReturnsSendFailed()
    {
        var service = new SmtpEmailService(Options());
        var result = await service.SendEmailAsync(EmailRequestBuilder.New().AddTo("ada@example.com").SetTextBody("hi"), TestCt);
        Assert.False(result.IsSuccess);
        Assert.Equal(EmailErrorCodes.SendFailed, result.Errors![0].Code);
    }

    [Fact]
    public async Task SendEmailAsync_Cancelled_ReturnsCancelledResult()
    {
        var service = new SmtpEmailService(Options());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var result = await service.SendEmailAsync(EmailRequestBuilder.New().AddTo("ada@example.com").SetTextBody("hi"), cts.Token);
        Assert.False(result.IsSuccess);
        Assert.Equal(EmailErrorCodes.OperationCancelled, result.Errors![0].Code);
    }

    [Fact]
    public async Task SendBulkEmailAsync_Empty_ReturnsEmpty()
    {
        var service = new SmtpEmailService(Options());
        var results = await service.SendBulkEmailAsync([], TestCt);
        Assert.Empty(results);
    }

    [Fact]
    public async Task SendBulkEmailAsync_UnreachableHost_MarksEveryMessageSendFailed()
    {
        var service = new SmtpEmailService(Options());
        EmailRequestBuilder[] builders = [EmailRequestBuilder.New().AddTo("a@example.com").SetTextBody("a"), EmailRequestBuilder.New().AddTo("b@example.com").SetTextBody("b")];
        var results = await service.SendBulkEmailAsync(builders, TestCt);
        Assert.Equal(2, results.Count);
        Assert.All(results, result => Assert.Equal(EmailErrorCodes.SendFailed, result.Errors![0].Code));
    }

    [Fact]
    public async Task SendBulkEmailAsync_CancelledBeforeConnect_MarksEveryMessageCancelled()
    {
        var service = new SmtpEmailService(Options());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        EmailRequestBuilder[] builders = [EmailRequestBuilder.New().AddTo("a@example.com").SetTextBody("a"), EmailRequestBuilder.New().AddTo("b@example.com").SetTextBody("b")];
        var results = await service.SendBulkEmailAsync(builders, cts.Token);
        Assert.Equal(2, results.Count);
        Assert.All(results, result => Assert.Equal(EmailErrorCodes.OperationCancelled, result.Errors![0].Code));
    }

    [Fact]
    public async Task TestConnectionAsync_Cancelled_Throws()
    {
        var service = new SmtpEmailService(Options());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await Assert.ThrowsAsync<OperationCanceledException>(() => service.TestConnectionAsync(cts.Token));
    }

    [Fact]
    public async Task TestConnectionAsync_UnreachableHost_ReturnsFalse()
    {
        var service = new SmtpEmailService(Options());
        var ok = await service.TestConnectionAsync(TestCt);
        Assert.False(ok);
    }

    [Fact]
    public void Validate_BlankHost_Throws()
    {
        var options = Options();
        options.Host = " ";
        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Equal("Host", ex.ParamName);
    }

    [Fact]
    public void Validate_PortOutOfRange_Throws()
    {
        var options = Options();
        options.Port = 0;
        Assert.Throws<InvalidFormatException>(options.Validate);
    }

    [Fact]
    public void ToString_OmitsPassword()
    {
        var options = Options();
        options.Username = "mailer";
        options.Password = "secret-value";
        var text = options.ToString();
        Assert.Contains("mailer", text);
        Assert.DoesNotContain("secret-value", text);
    }

    private static SmtpEmailOptions Options()
        => new() { Host = "127.0.0.1", Port = 1, DefaultFromAddress = "from@example.com", DefaultFromName = "From" };
}
