using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Job.Tests;

public class JobWorkerContextTests
{
    [Fact]
    public void Context_ExposesRunLoggerTokenAndResults()
    {
        using var cts = new CancellationTokenSource();
        var run = CreateRun();
        var builder = new JobWorkerResultBuilder();
        var context = new TestJobWorkerContext(run, cts.Token, builder);

        Assert.Same(run, context.Run);
        Assert.NotNull(context.Logger);
        Assert.Equal(cts.Token, context.CancellationToken);
        Assert.Same(builder, context.Results);
    }

    [Fact]
    public void Context_CancellationToken_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        var context = new TestJobWorkerContext(CreateRun(), cts.Token, new JobWorkerResultBuilder());

        cts.Cancel();

        Assert.True(context.CancellationToken.IsCancellationRequested);
    }

    private static JobRunRes CreateRun() => new() {
        Id = Guid.NewGuid(),
        JobDefinitionId = Guid.NewGuid(),
        CreatedTimestamp = DateTime.UtcNow,
        State = Lyo.Job.Models.Enums.JobState.Running
    };

    private sealed class TestJobWorkerContext(JobRunRes run, CancellationToken ct, JobWorkerResultBuilder results) : IJobWorkerContext
    {
        public JobRunRes Run { get; } = run;

        public ILogger Logger { get; } = NullLogger.Instance;

        public CancellationToken CancellationToken { get; } = ct;

        public JobWorkerResultBuilder Results { get; } = results;

        public Task ReportProgressAsync(int percent, string? message = null, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<JobRunRes>> CreateChildRunsAsync(JobCreateChildRunsReq request, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<JobRunRes>>([]);
    }
}
