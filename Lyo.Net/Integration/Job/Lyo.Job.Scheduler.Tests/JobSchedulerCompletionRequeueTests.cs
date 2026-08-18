using System.Reflection;
using System.Text;
using System.Text.Json;
using Lyo.Formatter;
using Lyo.Job.Models;
using Lyo.Job.Models.Response;
using Lyo.MessageQueue;

namespace Lyo.Job.Scheduler.Tests;

/// <summary>
/// Completion-queue poison handling: 404 acks, 500s use a counted envelope requeue (max 3) instead of an unbounded broker nack, and unparseable bytes are dropped.
/// </summary>
public class JobSchedulerCompletionRequeueTests
{
    [Fact]
    public async Task OnJobRunComplete_WhenRunGetReturns404_AcksWithoutRepublish()
    {
        var api = new FakeSchedulerApiClient(BuildDefinition()) { ThrowStatusOnRunGet = 404 };
        var mq = new RecordingDelayedMqService();
        var scheduler = CreateScheduler(api, mq);
        var requeue = await InvokeOnJobRunCompleteAsync(scheduler, JsonSerializer.SerializeToUtf8Bytes(Guid.NewGuid()));
        Assert.False(requeue);
        Assert.Empty(mq.DelayedSends);
        Assert.Empty(mq.QueueSends);
    }

    [Fact]
    public async Task OnJobRunComplete_WhenRunGetReturns500_RepublishesEnvelopeCount1()
    {
        var runId = Guid.NewGuid();
        var api = new FakeSchedulerApiClient(BuildDefinition()) { ThrowStatusOnRunGet = 500 };
        var mq = new RecordingDelayedMqService();
        var scheduler = CreateScheduler(api, mq);
        var requeue = await InvokeOnJobRunCompleteAsync(scheduler, JsonSerializer.SerializeToUtf8Bytes(runId));
        Assert.False(requeue);
        Assert.Empty(mq.QueueSends);
        var delayed = Assert.Single(mq.DelayedSends);
        Assert.Equal(Constants.Mq.QueueJobRunFinish, delayed.QueueName);
        Assert.Equal(TimeSpan.FromSeconds(2), delayed.Delay);
        var envelope = JsonSerializer.Deserialize<QueueMessageEnvelope<Guid>>(delayed.Data);
        Assert.NotNull(envelope);
        Assert.Equal(runId, envelope.Payload);
        Assert.Equal(1, envelope.RequeueCount);
    }

    [Fact]
    public async Task OnJobRunComplete_WhenEnvelopeAtMaxRequeueAnd500_DropsWithoutRepublish()
    {
        var runId = Guid.NewGuid();
        var api = new FakeSchedulerApiClient(BuildDefinition()) { ThrowStatusOnRunGet = 500 };
        var mq = new RecordingDelayedMqService();
        var scheduler = CreateScheduler(api, mq);
        var body = JsonSerializer.SerializeToUtf8Bytes(new QueueMessageEnvelope<Guid>(runId, JobScheduler.MaxRequeueCount, Guid.NewGuid().ToString("D"), DateTime.UtcNow));
        var requeue = await InvokeOnJobRunCompleteAsync(scheduler, body);
        Assert.False(requeue);
        Assert.Empty(mq.DelayedSends);
        Assert.Empty(mq.QueueSends);
    }

    [Fact]
    public async Task OnJobRunComplete_WhenBodyUnparseable_AcksWithoutRepublish()
    {
        var api = new FakeSchedulerApiClient(BuildDefinition()) { ThrowStatusOnRunGet = 500 };
        var mq = new RecordingDelayedMqService();
        var scheduler = CreateScheduler(api, mq);
        var requeue = await InvokeOnJobRunCompleteAsync(scheduler, Encoding.UTF8.GetBytes("not-a-guid"));
        Assert.False(requeue);
        Assert.Empty(mq.DelayedSends);
        Assert.Empty(mq.QueueSends);
    }

    [Fact]
    public async Task OnJobRunComplete_WhenNoMqAnd500_AcksWithoutThrowing()
    {
        var api = new FakeSchedulerApiClient(BuildDefinition()) { ThrowStatusOnRunGet = 500 };
        var scheduler = CreateScheduler(api, null);
        var requeue = await InvokeOnJobRunCompleteAsync(scheduler, JsonSerializer.SerializeToUtf8Bytes(Guid.NewGuid()));
        Assert.False(requeue);
    }

    private static JobScheduler CreateScheduler(FakeSchedulerApiClient api, IMqService? mqService)
        => new(
            new() {
                ApiBaseUrl = "http://localhost/api",
                DefinitionRefreshIntervalSeconds = 3600,
                ScheduleCheckIntervalSeconds = 3600,
                EnableMisfireCatchUp = false
            }, api, new FormatterService(), new FakeEventPublisher(), mqService: mqService);

    private static JobDefinitionRes BuildDefinition()
        => new(Guid.NewGuid(), "CompletionDef", null, "Test", "cs", true, [], [], [], null, 0, 0);

    private static async Task<bool> InvokeOnJobRunCompleteAsync(JobScheduler scheduler, byte[] body)
    {
        var method = typeof(JobScheduler).GetMethod("OnJobRunCompleteAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return await (Task<bool>)method.Invoke(scheduler, [body])!;
    }
}
