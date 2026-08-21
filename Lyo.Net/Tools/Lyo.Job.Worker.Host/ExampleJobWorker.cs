using Lyo.Job.Client;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Events;
using Lyo.Job.Models.Extensions;
using Lyo.Job.Models.Security;
using Lyo.MessageQueue;
using Lyo.Metrics;

namespace Lyo.Job.Worker.Host;

/// <summary>
/// Test worker that consumes <c>job.run.example</c>. Sleeps <c>DelaySeconds</c> (default 2), reports progress, then succeeds. Run against Test API so Test Gateway Jobs can
/// exercise dispatch, cancellation, and the worker registry.
/// </summary>
public sealed class ExampleJobWorker(
    IMqService mq,
    IJobClient jobClient,
    IJobEventPublisher events,
    string workerType,
    ILogger<ExampleJobWorker>? logger = null,
    IMetrics? metrics = null,
    int? maxRequeueCount = null,
    string? dlqName = null,
    IJobParameterEncryptionService? parameterEncryption = null)
    : JobWorkerBase(mq, jobClient, events, workerType, logger, metrics, maxRequeueCount, dlqName, parameterEncryption)
{
    protected override async Task ExecuteAsync(IJobWorkerContext ctx)
    {
        var delaySeconds = Math.Clamp(ctx.Run.JobRunParameters.GetInt(Constants.DelaySecondsParameterKey) ?? 2, 0, 60);
        await ctx.ReportProgressAsync(10, $"Sleeping {delaySeconds}s", ctx.CancellationToken).ConfigureAwait(false);
        if (delaySeconds > 0)
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), ctx.CancellationToken).ConfigureAwait(false);

        ctx.Results.AddResult("Message", $"Example worker finished after {delaySeconds}s", JobParameterType.String);
        await ctx.ReportProgressAsync(100, "Complete", ctx.CancellationToken).ConfigureAwait(false);
    }
}
