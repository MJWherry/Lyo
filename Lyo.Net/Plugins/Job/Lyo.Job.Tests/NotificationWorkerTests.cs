using System.Net;
using System.Text.Json;
using Lyo.Common.Core.Identifiers;
using Lyo.Common.Metadata.Records;
using Lyo.Email;
using Lyo.Email.Builders;
using Lyo.Email.Models;
using Lyo.Http.Client;
using Lyo.Job.Client;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Tests.Postgres;
using Lyo.Job.Worker;
using Lyo.Job.Workers.Notification;
using Lyo.Result;
using Lyo.Sms;
using Lyo.Sms.Builders;
using Lyo.Sms.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NotificationConstants = Lyo.Job.Workers.Notification.Constants;

namespace Lyo.Job.Tests;

/// <summary>
/// These workers exist so notifying is a workflow step instead of a column on every job definition. The point of every test here is that a send which does not happen fails
/// the run: the old <c>AlertWebhookUrl</c> path logged and moved on, which made a dead webhook invisible.
/// </summary>
public class NotificationWorkerTests
{
    [Fact]
    public async Task Webhook_Returns200_ReportsSuccessResult()
    {
        var http = new StubHttpClient(HttpStatusCode.OK);
        var ctx = Context(("Url", "https://hooks.example/notify"), ("Body", "{\"ok\":true}"));
        await new TestWebhookWorker(http).RunAsync(ctx);
        Assert.Equal(JobRunResult.Success, ctx.Results.CurrentOutcome);
        Assert.Equal("https://hooks.example/notify", http.LastUri);
        Assert.Equal("200", ResultValue(ctx, NotificationConstants.ResultKey.StatusCode));
    }

    [Fact]
    public async Task Webhook_Returns500_FailsTheRun()
    {
        var http = new StubHttpClient(HttpStatusCode.InternalServerError);
        var ctx = Context(("Url", "https://hooks.example/notify"), ("Body", "{}"));
        await new TestWebhookWorker(http).RunAsync(ctx);

        // A failed send must be a failed run. Reporting success here is what made the old alert-webhook column untrustworthy.
        Assert.Equal(JobRunResult.Failure, ctx.Results.CurrentOutcome);
    }

    [Fact]
    public async Task Webhook_Returns500_RetriesUpToTheConfiguredCount()
    {
        var http = new StubHttpClient(HttpStatusCode.InternalServerError);
        var ctx = Context(("Url", "https://hooks.example/notify"), ("Body", "{}"));
        await new TestWebhookWorker(http, new() { RetryCount = 2, RetryDelay = TimeSpan.Zero }).RunAsync(ctx);
        Assert.Equal(3, http.Attempts);
    }

    [Fact]
    public async Task Webhook_Returns400_DoesNotRetry()
    {
        var http = new StubHttpClient(HttpStatusCode.BadRequest);
        var ctx = Context(("Url", "https://hooks.example/notify"), ("Body", "{}"));
        await new TestWebhookWorker(http, new() { RetryCount = 3, RetryDelay = TimeSpan.Zero }).RunAsync(ctx);

        // A rejected payload fails the same way every time; retrying only delays the failure and multiplies the load.
        Assert.Equal(1, http.Attempts);
        Assert.Equal(JobRunResult.Failure, ctx.Results.CurrentOutcome);
    }

    [Fact]
    public async Task Webhook_MissingUrlParameter_FailsWithValidationMessage()
    {
        var http = new StubHttpClient(HttpStatusCode.OK);
        var ctx = Context(("Body", "{}"));
        await new TestWebhookWorker(http).RunAsync(ctx);
        Assert.Equal(JobRunResult.Failure, ctx.Results.CurrentOutcome);
        Assert.Equal(0, http.Attempts);
        Assert.Contains(NotificationConstants.ParameterKey.Url, FailureReason(ctx), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Webhook_TemplatedBody_UsesResolvedParameterValue()
    {
        var http = new StubHttpClient(HttpStatusCode.OK);

        // The scheduler resolved {Step.Extract.Result.RowCount} before the run was created, so the worker must post the value as-is rather than re-formatting it.
        var ctx = Context(("Url", "https://hooks.example/notify"), ("Body", "{\"rows\":42}"));
        await new TestWebhookWorker(http).RunAsync(ctx);
        Assert.Equal("{\"rows\":42}", http.LastBody);
    }

    [Fact]
    public async Task Email_SendSucceeds_ReportsRecipient()
    {
        var email = new StubEmailService();
        var ctx = Context(("To", "ops@example.com"), ("Subject", "Nightly ETL"), ("Body", "12 rows loaded"));
        await new TestEmailWorker(email).RunAsync(ctx);
        Assert.Equal(JobRunResult.Success, ctx.Results.CurrentOutcome);
        Assert.Equal("ops@example.com", ResultValue(ctx, NotificationConstants.ResultKey.Recipient));
    }

    [Fact]
    public async Task Email_ServiceThrows_FailsTheRun()
    {
        var email = new StubEmailService { Throw = new InvalidOperationException("SMTP refused") };
        var ctx = Context(("To", "ops@example.com"), ("Subject", "Nightly ETL"), ("Body", "12 rows loaded"));
        await new TestEmailWorker(email).RunAsync(ctx);

        // Routing through INotificationPublisher would have swallowed this and marked the run Success.
        Assert.Equal(JobRunResult.Failure, ctx.Results.CurrentOutcome);
        Assert.Contains("SMTP refused", FailureReason(ctx), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Email_ServiceReturnsFailure_FailsTheRun()
    {
        var email = new StubEmailService { Failure = "Mailbox unavailable" };
        var ctx = Context(("To", "ops@example.com"), ("Subject", "Nightly ETL"), ("Body", "hi"));
        await new TestEmailWorker(email).RunAsync(ctx);
        Assert.Equal(JobRunResult.Failure, ctx.Results.CurrentOutcome);
    }

    [Fact]
    public async Task Email_MissingTo_FailsWithValidationMessage()
    {
        var email = new StubEmailService();
        var ctx = Context(("Subject", "Nightly ETL"), ("Body", "hi"));
        await new TestEmailWorker(email).RunAsync(ctx);
        Assert.Equal(JobRunResult.Failure, ctx.Results.CurrentOutcome);
        Assert.False(email.Sent);
        Assert.Contains(NotificationConstants.ParameterKey.To, FailureReason(ctx), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sms_SendSucceeds_ReportsRecipient()
    {
        var sms = new StubSmsService();
        var ctx = Context(("To", "+15555550123"), ("Body", "ETL finished"));
        await new TestSmsWorker(sms).RunAsync(ctx);
        Assert.Equal(JobRunResult.Success, ctx.Results.CurrentOutcome);
        Assert.Equal("ETL finished", sms.LastBody);
    }

    [Fact]
    public async Task Sms_ServiceReturnsFailure_FailsTheRun()
    {
        var sms = new StubSmsService { Failure = "Carrier rejected" };
        var ctx = Context(("To", "+15555550123"), ("Body", "ETL finished"));
        await new TestSmsWorker(sms).RunAsync(ctx);
        Assert.Equal(JobRunResult.Failure, ctx.Results.CurrentOutcome);
    }

    [Fact]
    public async Task Sms_MissingBody_FailsWithValidationMessage()
    {
        var sms = new StubSmsService();
        var ctx = Context(("To", "+15555550123"));
        await new TestSmsWorker(sms).RunAsync(ctx);
        Assert.Equal(JobRunResult.Failure, ctx.Results.CurrentOutcome);
        Assert.Contains(NotificationConstants.ParameterKey.Body, FailureReason(ctx), StringComparison.Ordinal);
    }

    private static FakeWorkerContext Context(params (string Key, string Value)[] parameters)
    {
        var runId = LyoGuid.CreateV7();
        var run = new JobRunRes {
            Id = runId,
            JobDefinitionId = LyoGuid.CreateV7(),
            State = JobState.Running,
            CreatedTimestamp = DateTime.UtcNow,
            JobRunParameters = [.. parameters.Select(p => new JobRunParameterRes(LyoGuid.CreateV7(), runId, p.Key, LyoTypeInfo.String.FullName, p.Value, null, null, true))]
        };

        return new(run);
    }

    /// <summary>Results are stored as JSON, so a string value arrives quoted. Unwrap it, the way anything reading the run's results has to.</summary>
    private static string? ResultValue(FakeWorkerContext ctx, string key)
    {
        var raw = ctx.Results.Build().FirstOrDefault(r => string.Equals(r.Key, key, StringComparison.OrdinalIgnoreCase))?.Value?.ToString();
        return raw is null || !raw.StartsWith('"') ? raw : JsonSerializer.Deserialize<string>(raw);
    }

    private static string FailureReason(FakeWorkerContext ctx)
        => string.Join(" | ", ctx.Results.Build().Where(r => r.Key.StartsWith("FailureReason", StringComparison.OrdinalIgnoreCase)).Select(r => r.Value?.ToString()));

    /// <summary>
    /// A real <see cref="LyoHttpClient" /> over a recording handler, so the worker's retry and status handling run against the same non-success-throws behaviour production gets
    /// rather than against a hand-written double of it.
    /// </summary>
    private sealed class StubHttpClient : LyoHttpClient
    {
        private readonly RecordingHandler _handler;

        public int Attempts => _handler.Attempts;

        public string? LastUri => _handler.LastUri;

        public string? LastBody => _handler.LastBody;

        public StubHttpClient(HttpStatusCode status)
            : this(new RecordingHandler(status)) { }

        private StubHttpClient(RecordingHandler handler)
            : base(new(handler), logger: NullLogger.Instance)
            => _handler = handler;

        private sealed class RecordingHandler(HttpStatusCode status) : HttpMessageHandler
        {
            public int Attempts { get; private set; }

            public string? LastUri { get; private set; }

            public string? LastBody { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Attempts++;
                LastUri = request.RequestUri?.ToString();
                LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
                return new(status) { Content = new StringContent("") };
            }
        }
    }

    private sealed class StubEmailService : IEmailService
    {
        public Exception? Throw { get; set; }

        public string? Failure { get; set; }

        public bool Sent { get; private set; }

        public EmailRequest? LastRequest { get; private set; }

        public Task<EmailResult> SendEmailAsync(EmailRequest request, CancellationToken ct = default)
        {
            if (Throw is not null)
                throw Throw;

            Sent = true;
            LastRequest = request;
            return Task.FromResult(Failure is null ? EmailResult.FromSuccess(request, "ok") : EmailResult.FromError(Failure, request));
        }

        public Task<EmailResult> SendEmailAsync(EmailRequestBuilder requestBuilder, string fromAddress, string? fromName = null, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<EmailResult> SendEmailAsync(EmailRequestBuilder requestBuilder, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<EmailResult>> SendBulkEmailAsync(IEnumerable<EmailRequestBuilder> builders, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<BulkResult<EmailRequest>> SendBulkEmailAsync(BulkEmailRequestBuilder bulkRequestBuilder, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<bool> TestConnectionAsync(CancellationToken ct = default) => Task.FromResult(true);
    }

    private sealed class StubSmsService : ISmsService
    {
        public string? Failure { get; set; }

        public string? LastTo { get; private set; }

        public string? LastBody { get; private set; }

        public Task<Result<SmsRequest>> SendSmsAsync(string to, string body, string? from = null, CancellationToken ct = default)
        {
            LastTo = to;
            LastBody = body;
            var request = new SmsRequest(to, body, from);
            return Task.FromResult(Failure is null ? Result<SmsRequest>.Success(request) : Result<SmsRequest>.Failure(new Error(Failure, "send_failed")));
        }

        public Task<Result<SmsRequest>> SendMmsAsync(string to, IEnumerable<string> mediaUrls, string? body = null, string? from = null, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<Result<SmsRequest>> SendMmsAsync(string to, IEnumerable<Uri> mediaUrls, string? body = null, string? from = null, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<Result<SmsRequest>> SendAsync(SmsMessageBuilder builder, string? from = null, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<Result<SmsRequest>> SendAsync(SmsRequest request, CancellationToken ct = default) => SendSmsAsync(request.To ?? "", request.Body ?? "", request.From, ct);

        public Task<IReadOnlyList<Result<SmsRequest>>> SendBulkAsync(IEnumerable<SmsMessageBuilder> builders, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<Result<SmsRequest>>> SendBulkSmsAsync(IEnumerable<SmsRequest> messages, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<BulkResult<SmsRequest>> SendBulkAsync(BulkSmsBuilder bulkBuilder, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<Result<SmsRequest>> GetMessageByIdAsync(string messageId, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<SmsMessageQueryResults<Result<SmsRequest>>> GetMessagesAsync(SmsMessageQueryFilter filter, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<bool> TestConnectionAsync(CancellationToken ct = default) => Task.FromResult(true);
    }

    private sealed class NoopApiClient : StubApiClient;

    /// <summary>Exposes the protected <c>ExecuteAsync</c> and supplies the base-class services the notification workers never touch.</summary>
    private sealed class TestWebhookWorker(ILyoHttpClient http, WebhookNotificationWorkerOptions? options = null)
        : WebhookNotificationWorker(
            new FakeMqService(), new JobClient(new NoopApiClient()), new FakeJobEventPublisher(), http,
            options ?? new WebhookNotificationWorkerOptions { RetryDelay = TimeSpan.Zero }, NullLogger<WebhookNotificationWorker>.Instance)
    {
        public Task RunAsync(IJobWorkerContext ctx) => ExecuteAsync(ctx);
    }

    private sealed class TestEmailWorker(IEmailService email)
        : EmailNotificationWorker(new FakeMqService(), new JobClient(new NoopApiClient()), new FakeJobEventPublisher(), email, new(), NullLogger<EmailNotificationWorker>.Instance)
    {
        public Task RunAsync(IJobWorkerContext ctx) => ExecuteAsync(ctx);
    }

    private sealed class TestSmsWorker(ISmsService sms)
        : SmsNotificationWorker(new FakeMqService(), new JobClient(new NoopApiClient()), new FakeJobEventPublisher(), sms, new(), NullLogger<SmsNotificationWorker>.Instance)
    {
        public Task RunAsync(IJobWorkerContext ctx) => ExecuteAsync(ctx);
    }

    /// <summary>Minimal <see cref="IJobWorkerContext" />: the notification workers only read parameters, log, and record results.</summary>
    private sealed class FakeWorkerContext(JobRunRes run) : IJobWorkerContext
    {
        public JobRunRes Run { get; } = run;

        public ILogger Logger { get; } = NullLogger.Instance;

        public CancellationToken CancellationToken => TestCt;

        public JobWorkerResultBuilder Results { get; } = new();

        public Task ReportProgressAsync(int percent, string? message = null, CancellationToken ct = default) => Task.CompletedTask;

        public Task<IReadOnlyList<JobRunRes>> CreateChildRunsAsync(JobCreateChildRunsReq request, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<JobRunRes>>([]);

        public void AddContext(string name, object? value) { }

        public string Format(string template) => template;
    }
}