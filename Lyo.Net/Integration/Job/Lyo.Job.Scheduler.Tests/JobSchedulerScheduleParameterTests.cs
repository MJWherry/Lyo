using System.Reflection;
using Lyo.Common.Enums;
using Lyo.Formatter;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Schedule.Models;

namespace Lyo.Job.Scheduler.Tests;

/// <summary>
/// Schedule-level parameters must be merged into the run request when a schedule fires: they override definition defaults by key (case-insensitive), disabled
/// parameters are ignored, and String/Json values go through the same template formatting as definition/trigger parameters. Regression coverage for required
/// definition parameters with null defaults whose values live on the schedule (per-client schedules).
/// </summary>
public class JobSchedulerScheduleParameterTests
{
    [Fact]
    public async Task ScheduleParameters_OverrideDefinitionDefaultsByKey()
    {
        var definitionId = Guid.NewGuid();
        var clientId = Guid.NewGuid().ToString("D");
        var schedule = BuildSchedule(
            definitionId,
            new JobScheduleParameterRes(Guid.NewGuid(), Guid.NewGuid(), "ClientId", JobParameterType.Guid, clientId, "Klein client", null, Enabled: true),
            new JobScheduleParameterRes(Guid.NewGuid(), Guid.NewGuid(), "emailto", JobParameterType.String, "to@example.com", "Klein To", null, Enabled: true));

        var definition = BuildDefinition(
            definitionId, schedule,
            BuildDefinitionParameter(definitionId, "ClientId", JobParameterType.Guid, null, required: true),
            BuildDefinitionParameter(definitionId, "EmailTo", JobParameterType.String, null, required: true),
            BuildDefinitionParameter(definitionId, "PageSize", JobParameterType.Int, "200", required: false));

        var runReq = await BuildRunRequestAsync(definition, schedule);

        // Overridden by the schedule — exactly once per key, no empty definition default left alongside.
        var clientParam = Assert.Single(runReq.JobRunParameters, p => p.Key.Equals("ClientId", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(clientId, clientParam.Value);
        var emailParam = Assert.Single(runReq.JobRunParameters, p => p.Key.Equals("EmailTo", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("to@example.com", emailParam.Value);

        // Untouched definition default survives.
        var pageSizeParam = Assert.Single(runReq.JobRunParameters, p => p.Key == "PageSize");
        Assert.Equal("200", pageSizeParam.Value);
    }

    [Fact]
    public async Task DisabledScheduleParameters_AreIgnored()
    {
        var definitionId = Guid.NewGuid();
        var schedule = BuildSchedule(
            definitionId,
            new JobScheduleParameterRes(Guid.NewGuid(), Guid.NewGuid(), "EmailTo", JobParameterType.String, "to@example.com", null, null, Enabled: true),
            new JobScheduleParameterRes(Guid.NewGuid(), Guid.NewGuid(), "EmailToCc", JobParameterType.String, "cc@example.com", null, null, Enabled: false));

        var definition = BuildDefinition(
            definitionId, schedule,
            BuildDefinitionParameter(definitionId, "EmailTo", JobParameterType.String, null, required: true));

        var runReq = await BuildRunRequestAsync(definition, schedule);

        Assert.Single(runReq.JobRunParameters, p => p.Key == "EmailTo");
        Assert.DoesNotContain(runReq.JobRunParameters, p => p.Key == "EmailToCc");
    }

    [Fact]
    public async Task ScheduleParameterStringValues_AreTemplateFormatted()
    {
        var definitionId = Guid.NewGuid();
        var schedule = BuildSchedule(
            definitionId,
            new JobScheduleParameterRes(Guid.NewGuid(), Guid.NewGuid(), "Note", JobParameterType.String, "Run of {{Definition.Name}}", null, null, Enabled: true));

        var definition = BuildDefinition(definitionId, schedule);

        var runReq = await BuildRunRequestAsync(definition, schedule);

        var noteParam = Assert.Single(runReq.JobRunParameters, p => p.Key == "Note");
        Assert.Equal("Run of ScheduleParamDef", noteParam.Value);
    }

    /// <summary>Loads the definition into the scheduler cache via the fake API, then invokes the private BuildRunRequest as CreateScheduledRunAsync does for a due slot.</summary>
    private static async Task<JobRunReq> BuildRunRequestAsync(JobDefinitionRes definition, JobScheduleRes schedule)
    {
        var api = new FakeSchedulerApiClient(definition);
        var scheduler = new JobScheduler(
            new JobSchedulerOptions {
                ApiBaseUrl = "http://localhost/api",
                DefinitionRefreshIntervalSeconds = 3600,
                ScheduleCheckIntervalSeconds = 3600,
                EnableMisfireCatchUp = false
            },
            api,
            new FormatterService(),
            new FakeEventPublisher());

        await scheduler.RefreshDefinitionsAsync(TestContext.Current.CancellationToken);
        var method = typeof(JobScheduler).GetMethod("BuildRunRequest", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (JobRunReq)method.Invoke(scheduler, [definition.Id, schedule, null, null])!;
    }

    private static JobDefinitionRes BuildDefinition(Guid definitionId, JobScheduleRes schedule, params JobParameterRes[] parameters)
        => new(
            definitionId, "ScheduleParamDef", null, "Test", "cs", true,
            JobParameters: parameters, JobSchedules: [schedule], JobTriggers: [], JobParallelRestrictions: null);

    private static JobParameterRes BuildDefinitionParameter(Guid definitionId, string key, JobParameterType type, string? value, bool required)
        => new(Guid.NewGuid(), definitionId, key, null, type, value, null, AllowMultiple: false, Enabled: true, Required: required);

    private static JobScheduleRes BuildSchedule(Guid definitionId, params JobScheduleParameterRes[] parameters)
        => new(
            Guid.NewGuid(), definitionId, MonthFlags.None, DayFlags.None, ScheduleType.SetTimes,
            Times: [new(12, 0)], StartTime: null, EndTime: null, IntervalMinutes: null,
            Description: "test schedule", Enabled: true, Parameters: parameters);
}
