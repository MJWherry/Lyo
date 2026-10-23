using Lyo.Api.Models.Common.Response;
using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Identifiers;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Schedule.Models;
using Constants = Lyo.Job.Models.Constants;

namespace Lyo.Job.Scheduler.Tests;

/// <summary>
/// Workflow schedules reuse the definition scheduler's slot math but own their own dedupe: the workflow run, not the job run, carries the slot. These lock that a due slot
/// starts exactly one run, that a blackout defers instead of firing early, and that an in-flight run is not stacked on top of.
/// </summary>
public class JobWorkflowSchedulerTests
{
    private static JobBlackoutCalendarRes AllDayBlackout { get; } = new(
        LyoGuid.CreateV7(), "Always", null, true,
        [new(LyoGuid.CreateV7(), LyoGuid.CreateV7(), "AllDay", DayFlags.EveryDay, new(0, 0), new(23, 59), JobBlackoutPolicy.Defer, true)]);

    [Fact]
    public async Task CheckWorkflowSchedulesAsync_DueSlot_StartsWorkflowRunWithSlot()
    {
        var api = new FakeWorkflowSchedulerApiClient(HourlyCron());
        await CreateScheduler(api).CheckWorkflowSchedulesAsync(TestCt);
        var started = Assert.Single(api.StartRequests);
        Assert.Equal(api.Schedule.Id, started.Request!.JobScheduleId);
        Assert.Equal(CurrentHourUtc(), started.Request.ScheduledSlotUtc);
        Assert.Equal(api.Schedule.JobWorkflowId, started.WorkflowId);
    }

    [Fact]
    public async Task CheckWorkflowSchedulesAsync_SlotAlreadyStarted_DoesNotStartSecondRun()
    {
        var api = new FakeWorkflowSchedulerApiClient(HourlyCron());
        var scheduler = CreateScheduler(api);
        await scheduler.CheckWorkflowSchedulesAsync(TestCt);

        // Finish the run so the "already pending or running" guard cannot be what stops the second pass — the slot itself has to.
        api.LatestRun = api.LatestRun! with { State = JobWorkflowRunState.Finished, FinishedTimestamp = DateTime.UtcNow };
        await scheduler.CheckWorkflowSchedulesAsync(TestCt);

        // The slot is the dedupe key. A second check inside the same hour must not post again, or every poll interval would start a run.
        Assert.Single(api.StartRequests);
    }

    [Fact]
    public async Task CheckWorkflowSchedulesAsync_BlackoutWindow_DefersSlot()
    {
        var api = new FakeWorkflowSchedulerApiClient(HourlyCron() with { JobBlackoutCalendarId = AllDayBlackout.Id, JobBlackoutCalendar = AllDayBlackout });
        await CreateScheduler(api).CheckWorkflowSchedulesAsync(TestCt);

        // Deferring pushes the slot past now, so nothing is due yet. Firing anyway would run the workflow inside the window it was told to avoid.
        Assert.Empty(api.StartRequests);
    }

    [Fact]
    public async Task CheckWorkflowSchedulesAsync_RunAlreadyRunning_SkipsSlot()
    {
        var api = new FakeWorkflowSchedulerApiClient(HourlyCron()) {
            LatestRun = new(LyoGuid.CreateV7(), Guid.Empty, JobWorkflowRunState.Running, DateTime.UtcNow.AddMinutes(-5), null, DateTime.UtcNow.AddMinutes(-5), null)
        };

        await CreateScheduler(api).CheckWorkflowSchedulesAsync(TestCt);
        Assert.Empty(api.StartRequests);
    }

    [Fact]
    public async Task CheckWorkflowSchedulesAsync_DisabledSchedule_StartsNothing()
    {
        var api = new FakeWorkflowSchedulerApiClient(HourlyCron() with { Enabled = false });
        await CreateScheduler(api).CheckWorkflowSchedulesAsync(TestCt);
        Assert.Empty(api.StartRequests);
    }

    private static JobWorkflowScheduler CreateScheduler(FakeWorkflowSchedulerApiClient api) => new(new() { ApiBaseUrl = "http://localhost/api" }, api);

    private static DateTime CurrentHourUtc()
    {
        var now = DateTime.UtcNow;
        return new(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
    }

    private static JobScheduleRes HourlyCron()
        => new(
            LyoGuid.CreateV7(), null, MonthFlags.EveryMonth, DayFlags.EveryDay, ScheduleType.Cron, null, null, null, null, "hourly", true, null, "0 * * * *",
            JobWorkflowId: LyoGuid.CreateV7());
}

/// <summary>Answers the workflow scheduler's API calls in memory: the workflow-owned schedule query, the latest run per schedule, and the Start post (recorded).</summary>
internal sealed class FakeWorkflowSchedulerApiClient(JobScheduleRes schedule) : StubApiClient
{
    public JobScheduleRes Schedule { get; } = schedule;

    /// <summary>Most recent workflow run for the schedule, or null when it has never run.</summary>
    public JobWorkflowRunRes? LatestRun { get; set; }

    public List<(Guid WorkflowId, JobWorkflowStartReq? Request)> StartRequests { get; } = [];

    public override Task<TResult> PostAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        where TRequest : default
    {
        if (uri.Contains($"{Constants.Rest.Job.Schedules}/QueryConcrete", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult((TResult)(object)BuildQueryRes<JobScheduleRes>([Schedule]));

        if (uri.Contains($"{Constants.Rest.Job.WorkflowRuns}/QueryConcrete", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult((TResult)(object)BuildQueryRes<JobWorkflowRunRes>(LatestRun is null ? [] : [LatestRun]));

        if (uri.Contains("/Start", StringComparison.OrdinalIgnoreCase)) {
            var startReq = request as JobWorkflowStartReq;
            var workflowId = Guid.Parse(uri.Split('/')[^2]);
            StartRequests.Add((workflowId, startReq));
            var run = new JobWorkflowRunRes(
                LyoGuid.CreateV7(), workflowId, JobWorkflowRunState.Pending, null, null, DateTime.UtcNow, null, null, startReq?.JobScheduleId, startReq?.ScheduledSlotUtc);

            LatestRun = run;
            return Task.FromResult((TResult)(object)new CreateResult<JobWorkflowRunRes>(true, run, null));
        }

        throw new NotImplementedException(uri);
    }

    private static QueryRes<T> BuildQueryRes<T>(IReadOnlyList<T> items) => new(new(), true, items, 0, items.Count, items.Count, false, 0, null);
}