using Lyo.Parameters;
using Lyo.Common.Metadata.Records;
using Lyo.Formatter;
using Lyo.Job.Client;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Events;
using Lyo.Job.Models.Response;
using Lyo.Job.Tests.Postgres;
using Lyo.Job.Worker;
using Lyo.MessageQueue;
using Lyo.Result;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Job.Tests;

/// <summary>String parameter placeholders resolve against <c>jobrun</c> and <c>AddContext</c> objects through <see cref="IFormatterService"/>.</summary>
public class JobWorkerParameterFormatTests
{
    private static readonly DateTimeOffset Frozen = new(2026, 8, 22, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task StringParam_JobRunParameters_ResolvesBeforeExecute()
    {
        var api = new FormatApiClient(
            Param("StartDate", LyoTypeInfo.DateTime, LyoTypeInfo.DateTime.ToJson(new DateTime(2026, 8, 1))),
            Param("EmailSubject", LyoTypeInfo.String, LyoTypeInfo.String.ToJson("{jobrun.parameters.startdate:yyyy-MM-dd}")));
        var worker = CreateWorker(api);
        string? subject = null;
        await worker.InvokeDoWorkAsync(
            api.RunId, ctx => {
                subject = ctx.Run.JobRunParameters.GetString("EmailSubject");
                return Task.CompletedTask;
            });

        Assert.Equal(LyoTypeInfo.String.ToJson("2026-08-01"), subject);
    }

    [Fact]
    public async Task StringParam_ClientPlaceholder_ResolvesAfterAddContext()
    {
        var api = new FormatApiClient(Param("EmailTo", LyoTypeInfo.String, LyoTypeInfo.String.ToJson("{client.contact.emailAddress}")));
        var worker = CreateWorker(api);
        string? before = null;
        string? after = null;
        await worker.InvokeDoWorkAsync(
            api.RunId, ctx => {
                before = ctx.Run.JobRunParameters.GetString("EmailTo");
                ctx.AddContext("client", new { contact = new { emailAddress = "to@example.com" } });
                after = ctx.Run.JobRunParameters.GetString("EmailTo");
                return Task.CompletedTask;
            });

        Assert.Equal(LyoTypeInfo.String.ToJson("{client.contact.emailAddress}"), before);
        Assert.Equal(LyoTypeInfo.String.ToJson("to@example.com"), after);
    }

    [Fact]
    public async Task StringParam_PlaceholdersAndKeys_AreCaseInsensitive()
    {
        var api = new FormatApiClient(Param("EmailTo", LyoTypeInfo.String, LyoTypeInfo.String.ToJson("{CLIENT.contact.emailAddress}")));
        var worker = CreateWorker(api);
        string? to = null;
        await worker.InvokeDoWorkAsync(
            api.RunId, ctx => {
                ctx.AddContext("client", new { contact = new { emailAddress = "to@example.com" } });
                to = ctx.Run.JobRunParameters.GetString("emailto");
                return Task.CompletedTask;
            });

        Assert.Equal(LyoTypeInfo.String.ToJson("to@example.com"), to);
    }

    [Fact]
    public async Task JsonParam_WithBraces_IsLeftAlone()
    {
        const string json = "{\"name\":\"{client.name}\"}";
        var api = new FormatApiClient(Param("Payload", LyoTypeInfo.JsonNode, json));
        var worker = CreateWorker(api);
        string? payload = null;
        await worker.InvokeDoWorkAsync(
            api.RunId, ctx => {
                ctx.AddContext("client", new { name = "Acme" });
                payload = ctx.Run.JobRunParameters.GetString("Payload");
                return Task.CompletedTask;
            });

        Assert.Equal(json, payload);
    }

    [Fact]
    public async Task NoFormatter_LeavesTemplatesUnchanged()
    {
        var api = new FormatApiClient(Param("EmailTo", LyoTypeInfo.String, LyoTypeInfo.String.ToJson("{client.contact.emailAddress}")));
        var worker = CreateWorker(api, formatter: null);
        string? to = null;
        await worker.InvokeDoWorkAsync(
            api.RunId, ctx => {
                ctx.AddContext("client", new { contact = new { emailAddress = "to@example.com" } });
                to = ctx.Run.JobRunParameters.GetString("EmailTo");
                return Task.CompletedTask;
            });

        Assert.Equal(LyoTypeInfo.String.ToJson("{client.contact.emailAddress}"), to);
    }

    [Fact]
    public async Task ChainedParams_ResolveOnLaterPass()
    {
        var api = new FormatApiClient(
            Param("EmailTo", LyoTypeInfo.String, LyoTypeInfo.String.ToJson("{client.contact.emailAddress}")),
            Param("EmailSubject", LyoTypeInfo.String, LyoTypeInfo.String.ToJson("{jobrun.parameters.emailTo}")));
        var worker = CreateWorker(api);
        string? subject = null;
        await worker.InvokeDoWorkAsync(
            api.RunId, ctx => {
                ctx.AddContext("client", new { contact = new { emailAddress = "to@example.com" } });
                subject = ctx.Run.JobRunParameters.GetString("EmailSubject");
                return Task.CompletedTask;
            });

        Assert.Equal(LyoTypeInfo.String.ToJson("to@example.com"), subject);
    }

    [Fact]
    public async Task Expression_DateTimeUtcNowAddDays_UsesFormatterClock()
    {
        var api = new FormatApiClient(Param("EmailSubject", LyoTypeInfo.String, LyoTypeInfo.String.ToJson("Since {DateTime.UtcNow.AddDays(-1):yyyy-MM-dd}")));
        var worker = CreateWorker(api, new FormatterService(() => Frozen));
        string? subject = null;
        await worker.InvokeDoWorkAsync(
            api.RunId, ctx => {
                subject = ctx.Run.JobRunParameters.GetString("EmailSubject");
                return Task.CompletedTask;
            });

        Assert.Equal(LyoTypeInfo.String.ToJson("Since 2026-08-21"), subject);
    }

    [Fact]
    public async Task FormatterParam_ResolvesLikeString()
    {
        var api = new FormatApiClient(Param("When", FormatterLyoType.Template, FormatterLyoType.Template.ToJson("{DateTime.UtcNow:yyyy-MM-dd}")));
        var worker = CreateWorker(api, new FormatterService(() => Frozen));
        string? when = null;
        await worker.InvokeDoWorkAsync(
            api.RunId, ctx => {
                when = ctx.Run.JobRunParameters.GetString("When");
                return Task.CompletedTask;
            });

        Assert.Equal(FormatterLyoType.Template.ToJson("2026-08-22"), when);
    }

    private static TestJobWorker CreateWorker(FormatApiClient api, IFormatterService? formatter)
        => new(new FakeMqService(), new JobClient(api), new FakeJobEventPublisher()) { Formatter = formatter };

    private static TestJobWorker CreateWorker(FormatApiClient api) => CreateWorker(api, new FormatterService(() => Frozen));

    private static JobRunParameterRes Param(string key, LyoTypeInfo type, string value)
        => new(Guid.NewGuid(), Guid.NewGuid(), key, type.FullName, value, null, null, true);

    private sealed class TestJobWorker(IMqService mq, IJobClient jobClient, IJobEventPublisher events)
        : JobWorkerBase(mq, jobClient, events, "cs", NullLogger.Instance)
    {
        protected override TimeSpan HeartbeatInterval => TimeSpan.FromHours(1);

        private Func<IJobWorkerContext, Task> _execute = _ => Task.CompletedTask;

        protected override Task ExecuteAsync(IJobWorkerContext ctx) => _execute(ctx);

        public Task<Result<Unit>> InvokeDoWorkAsync(Guid runId, Func<IJobWorkerContext, Task> execute)
        {
            _execute = execute;
            return DoWorkAsync(runId, TestContext.Current.CancellationToken);
        }
    }

    private sealed class FormatApiClient(params JobRunParameterRes[] parameters) : StubApiClient
    {
        public Guid RunId { get; } = Guid.NewGuid();

        public override Task<TResult?> GetAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            where TResult : default
            => Task.FromResult((TResult?)(object?)BuildRun(JobState.Queued));

        public override Task<TResult> PostAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            => Task.FromResult((TResult)(object)BuildRun(uri.Contains("/Started", StringComparison.OrdinalIgnoreCase) ? JobState.Running : JobState.Finished));

        public override Task<TResult> PostAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            where TRequest : default
        {
            if (uri.Contains("/Started", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult((TResult)(object)BuildRun(JobState.Running));
            if (uri.Contains("/Finished", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult((TResult)(object)BuildRun(JobState.Finished));
            return Task.FromResult(default(TResult)!);
        }

        public override Task<TResult> PatchAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            where TRequest : default
            => Task.FromResult(default(TResult)!);

        private JobRunRes BuildRun(JobState state)
            => new() {
                Id = RunId,
                State = state,
                CreatedTimestamp = DateTime.UtcNow,
                JobDefinitionId = Guid.NewGuid(),
                JobDefinition = new(Guid.NewGuid(), "TestDef", null, "Test", "cs", true, null, null, null, null),
                JobRunParameters = parameters.Select(p => p with { JobRunId = RunId }).ToList()
            };
    }
}
