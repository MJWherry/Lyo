using Lyo.Email.Builders;
using Lyo.Email.Models;
using Lyo.Exceptions.Models;
using Lyo.Metrics;
using Lyo.Metrics.Models;

namespace Lyo.Email.Tests;

public class EmailServiceBaseTests
{
    private readonly TestEmailOptions _options = new();

    [Fact]
    public async Task SendEmailAsync_BuilderWithoutFrom_UsesDefaultFromOnFailure()
    {
        var service = new FakeEmailService(_options) { Fail = true };
        var result = await service.SendEmailAsync(EmailRequestBuilder.New().SetSubject("Test").SetTextBody("Body").AddTo("recipient@example.com"), TestCt);
        Assert.False(result.IsSuccess);
        Assert.Equal(_options.DefaultFromAddress, result.Data!.FromAddress);
        Assert.Equal(_options.DefaultFromName, result.Data.FromName);
    }

    [Fact]
    public async Task SendEmailAsync_ExplicitFrom_ReplacesBuilderFrom()
    {
        var service = new FakeEmailService(_options);
        var builder = EmailRequestBuilder.New().SetSubject("Test").SetTextBody("Body").SetFrom("custom@example.com", "Custom").AddTo("recipient@example.com");
        var result = await service.SendEmailAsync(builder, "override@example.com", "Override", TestCt);
        Assert.Equal("override@example.com", result.Data!.FromAddress);
        Assert.Equal("Override", result.Data.FromName);
    }

    [Fact]
    public async Task SendEmailAsync_RequestWithoutFrom_LeavesFromNull()
    {
        var service = new FakeEmailService(_options) { Fail = true };
        var request = new EmailRequest(ToAddresses: ["recipient@example.com"], Subject: "Request", TextBody: "Plain", HtmlBody: "<p>Html</p>");
        var result = await service.SendEmailAsync(request, TestCt);
        Assert.Null(result.Data!.FromAddress);
        Assert.Null(result.Data.FromName);
        Assert.Equal("Plain", result.Data.TextBody);
        Assert.Equal("<p>Html</p>", result.Data.HtmlBody);
        Assert.Null(service.Sent[0].FromAddress);
    }

    [Fact]
    public async Task SendEmailAsync_TooManyAttachments_ThrowsBeforeSend()
    {
        _options.MaxAttachmentCountPerEmail = 1;
        var service = new FakeEmailService(_options);
        var builder = EmailRequestBuilder.New().AddTo("recipient@example.com").AddAttachment("a.txt", [1]).AddAttachment("b.txt", [2]);
        await Assert.ThrowsAsync<ArgumentOutsideRangeException>(() => service.SendEmailAsync(builder, TestCt));
        Assert.Empty(service.Sent);
    }

    [Fact]
    public async Task SendBulkEmailAsync_OverLimit_Throws()
    {
        _options.MaxBulkEmailLimit = 1;
        var service = new FakeEmailService(_options);
        EmailRequestBuilder[] builders = [EmailRequestBuilder.New().AddTo("a@example.com").SetTextBody("a"), EmailRequestBuilder.New().AddTo("b@example.com").SetTextBody("b")];
        await Assert.ThrowsAsync<ArgumentOutsideRangeException>(() => service.SendBulkEmailAsync(builders, TestCt));
        Assert.Empty(service.Sent);
    }

    [Fact]
    public async Task SendBulkEmailAsync_Empty_ReturnsEmptyWithoutSending()
    {
        var service = new FakeEmailService(_options);
        var results = await service.SendBulkEmailAsync([], TestCt);
        Assert.Empty(results);
        Assert.Empty(service.Sent);
    }

    [Fact]
    public async Task SendEmailAsync_Cancelled_ReturnsCancelledResult()
    {
        var service = new FakeEmailService(_options);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var result = await service.SendEmailAsync(EmailRequestBuilder.New().AddTo("a@example.com").SetTextBody("Body"), cts.Token);
        Assert.False(result.IsSuccess);
        Assert.Equal(EmailErrorCodes.OperationCancelled, result.Errors![0].Code);
    }

    [Fact]
    public async Task EmailSent_FiresWhenSendFails()
    {
        var service = new FakeEmailService(_options) { Fail = true };
        EmailSentEventArgs? sent = null;
        service.EmailSent += (_, args) => sent = args;
        await service.SendEmailAsync(EmailRequestBuilder.New().AddTo("a@example.com").SetTextBody("Body"), TestCt);
        Assert.NotNull(sent);
        Assert.False(sent.EmailResult.IsSuccess);
    }

    [Fact]
    public void Validate_MissingFrom_Throws()
    {
        var options = new TestEmailOptions { DefaultFromAddress = "  " };
        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Equal("DefaultFromAddress", ex.ParamName);
    }

    [Fact]
    public void Validate_AttachmentLimit_Throws()
    {
        var options = new TestEmailOptions { MaxAttachmentCountPerEmail = 0 };
        Assert.Throws<ArgumentOutsideRangeException>(options.Validate);
    }

    [Fact]
    public async Task EnableMetricsFalse_IgnoresInjectedMetrics()
    {
        var metrics = new CountingMetrics();
        var service = new FakeEmailService(_options, metrics);
        await service.SendEmailAsync(EmailRequestBuilder.New().AddTo("a@example.com").SetTextBody("Body"), TestCt);
        Assert.Equal(0, metrics.Counters);
    }

    private sealed class TestEmailOptions : EmailServiceOptions
    {
        public TestEmailOptions()
        {
            DefaultFromAddress = "test@example.com";
            DefaultFromName = "Test Sender";
        }
    }

    private sealed class FakeEmailService : EmailServiceBase
    {
        public FakeEmailService(EmailServiceOptions options, IMetrics? metrics = null)
            : base(options, metrics: metrics) { }

        public bool Fail { get; init; }

        public List<EmailRequest> Sent { get; } = [];

        protected override Task<EmailResult> SendCoreAsync(EmailRequest request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Sent.Add(request);
            if (Fail)
                throw new InvalidOperationException("nope");

            return Task.FromResult(EmailResult.FromSuccess(request, "ok"));
        }

        protected override Task<bool> TestConnectionCoreAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(true);
        }
    }

    private sealed class CountingMetrics : IMetrics
    {
        public int Counters { get; private set; }

        public void IncrementCounter(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null) => Counters++;

        public void DecrementCounter(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null) { }

        public void RecordGauge(string name, IConvertible value, IEnumerable<(string, string)>? tags = null) { }

        public void RecordHistogram(string name, IConvertible value, IEnumerable<(string, string)>? tags = null) { }

        public void RecordTiming(string name, TimeSpan duration, IEnumerable<(string, string)>? tags = null) { }

        public void RecordError(string name, Exception exception, IEnumerable<(string, string)>? tags = null) { }

        public MetricsTimer StartTimer(string name, IEnumerable<(string, string)>? tags = null) => default;

        public void RecordEvent(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null) { }
    }
}
