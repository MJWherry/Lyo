using Lyo.Common.Core.Enums;
using Lyo.Formatter;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Response;
using Lyo.Schedule.Models;

namespace Lyo.Job.Scheduler.Tests;

/// <summary>
/// A finished run for the current cron slot must not be re-created every schedule check. Preferring last success over that slot's timestamp was the unique-constraint loop:
/// GetDueSlot kept returning the same instant, Create posted, Postgres 23505'd, the API 500'd, and the scheduler tried again on the next tick.
/// </summary>
public class JobSchedulerDueSlotTests
{
    [Fact]
    public async Task CheckSchedules_WhenLastRunAlreadyOwnsTheDueSlot_DoesNotCreateAgain()
    {
        var definitionId = Guid.NewGuid();
        var schedule = HourlyCron(definitionId);
        var definition = Definition(definitionId, schedule);
        var currentHour = CurrentHourUtc();
        var api = new FakeSchedulerApiClient(definition) {
            LastSuccessfulRun = Run(definitionId, schedule.Id, currentHour.AddHours(-1), JobRunResult.Success),
            LastRun = Run(definitionId, schedule.Id, currentHour, JobRunResult.Failure)
        };

        var scheduler = await StartAsync(api);
        await scheduler.CheckSchedulesAsync(TestContext.Current.CancellationToken);
        Assert.Empty(api.CreatedRunRequests);
    }

    [Fact]
    public async Task CheckSchedules_WhenLastSuccessIsThePreviousSlot_CreatesTheCurrentHour()
    {
        var definitionId = Guid.NewGuid();
        var schedule = HourlyCron(definitionId);
        var definition = Definition(definitionId, schedule);
        var currentHour = CurrentHourUtc();
        var api = new FakeSchedulerApiClient(definition) {
            LastSuccessfulRun = Run(definitionId, schedule.Id, currentHour.AddHours(-1), JobRunResult.Success),
            LastRun = Run(definitionId, schedule.Id, currentHour.AddHours(-1), JobRunResult.Success)
        };

        var scheduler = await StartAsync(api);
        await scheduler.CheckSchedulesAsync(TestContext.Current.CancellationToken);
        var created = Assert.Single(api.CreatedRunRequests);
        Assert.Equal(schedule.Id, created.JobScheduleId);
        Assert.Equal(currentHour, created.ScheduledSlotUtc);
    }

    [Fact]
    public async Task CheckSchedules_AfterRefreshReplacesLastRunWithARetry_DoesNotRePostTheKnownSlot()
    {
        var definitionId = Guid.NewGuid();
        var schedule = HourlyCron(definitionId);
        var definition = Definition(definitionId, schedule);
        var currentHour = CurrentHourUtc();
        var api = new FakeSchedulerApiClient(definition) {
            LastSuccessfulRun = Run(definitionId, schedule.Id, currentHour.AddHours(-1), JobRunResult.Success),
            LastRun = Run(definitionId, schedule.Id, currentHour, JobRunResult.Failure)
        };

        var scheduler = await StartAsync(api);
        await scheduler.CheckSchedulesAsync(TestContext.Current.CancellationToken);
        Assert.Empty(api.CreatedRunRequests);

        // Refresh reloads LastRun as a newer failed retry (no schedule id). The process must still remember the cron slot.
        var retry = new JobRunRes {
            Id = Guid.NewGuid(),
            JobDefinitionId = definitionId,
            State = JobState.Finished,
            Result = JobRunResult.Failure,
            CreatedTimestamp = DateTime.UtcNow,
            ScheduledSlotUtc = DateTime.UtcNow.AddSeconds(30)
        };
        api.LastRun = retry;
        api.LastFailedRun = retry;
        api.LastSuccessfulRun = Run(definitionId, schedule.Id, currentHour.AddHours(-1), JobRunResult.Success);
        await scheduler.RefreshDefinitionsAsync(TestContext.Current.CancellationToken);
        await scheduler.CheckSchedulesAsync(TestContext.Current.CancellationToken);
        Assert.Empty(api.CreatedRunRequests);
    }

    private static async Task<JobScheduler> StartAsync(FakeSchedulerApiClient api)
    {
        var scheduler = new JobScheduler(
            new() {
                ApiBaseUrl = "http://localhost/api",
                DefinitionRefreshIntervalSeconds = 3600,
                ScheduleCheckIntervalSeconds = 3600,
                EnableMisfireCatchUp = false
            }, api, new FormatterService(), new FakeEventPublisher());

        await scheduler.RefreshDefinitionsAsync(TestContext.Current.CancellationToken);
        return scheduler;
    }

    private static DateTime CurrentHourUtc()
    {
        var now = DateTime.UtcNow;
        return new(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
    }

    private static JobDefinitionRes Definition(Guid definitionId, JobScheduleRes schedule)
        => new(definitionId, "DueSlotDef", null, "Test", "cs", true, [], [schedule], [], null);

    private static JobScheduleRes HourlyCron(Guid definitionId)
        => new(Guid.NewGuid(), definitionId, MonthFlags.EveryMonth, DayFlags.EveryDay, ScheduleType.Cron, null, null, null, null, "hourly", true, null, "0 * * * *");

    private static JobRunRes Run(Guid definitionId, Guid scheduleId, DateTime slot, JobRunResult result)
        => new() {
            Id = Guid.NewGuid(),
            JobDefinitionId = definitionId,
            JobScheduleId = scheduleId,
            State = JobState.Finished,
            Result = result,
            CreatedTimestamp = slot.AddSeconds(1),
            StartedTimestamp = slot.AddSeconds(2),
            FinishedTimestamp = slot.AddSeconds(10),
            ScheduledSlotUtc = slot
        };
}
