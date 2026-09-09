using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.Records;
using Lyo.Formatter;
using Lyo.Job.Models;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Response;
using Lyo.Job.Web.Components;
using Lyo.Query.Models.Enums;
using Lyo.Schedule.Models;

namespace Lyo.Job.Scheduler.Tests;

/// <summary>
/// The template editor offers the keys from <see cref="JobFormatterContext" />, but the keys that actually resolve come from
/// <c>JobScheduler.BuildTemplateData</c>. These assert the two agree, so a scheduler change cannot leave the editor advertising tokens that resolve to nothing (or
/// hiding ones that would work).
/// </summary>
/// <remarks>
/// These live here instead of in <c>Lyo.Job.Tests</c> because the scheduler side of the comparison needs a real <see cref="JobScheduler" />, and the fakes for that
/// already exist in this project.
/// </remarks>
public class JobFormatterContextTests
{
    [Fact]
    public void Build_WithoutRuns_CoversEverySchedulerKey()
    {
        var definition = BuildDefinition();
        var schedule = definition.JobSchedules![0];

        var scheduler = SchedulerKeys(new(definition), definition.JobTriggers![0], null, schedule);
        var editor = JobFormatterContext.Build(definition, schedule: schedule);

        AssertCovers(scheduler, editor);
    }

    [Fact]
    public void Build_WithLatestRuns_CoversFlattenedRunKeys()
    {
        var definition = BuildDefinition();
        var schedule = definition.JobSchedules![0];
        var run = BuildRun(definition.Id);
        var latest = new JobDefinitionLatestRunsRes {
            JobDefinitionId = definition.Id,
            LastRun = run,
            LastSuccessfulRun = run,
            LastFailedRun = run
        };

        var scheduler = SchedulerKeys(new(definition, run, run, run), definition.JobTriggers![0], run, schedule);
        var editor = JobFormatterContext.Build(definition, latest, schedule);

        AssertCovers(scheduler, editor);
        Assert.Contains("LastRun_Result_RowCount", editor.Keys);
        Assert.Contains("LastRun_Parameter_EmailTo", editor.Keys);
    }

    /// <summary>Run paths have to resolve even for a definition that has never run, otherwise a new definition offers no autocomplete at all.</summary>
    [Fact]
    public void Build_WithoutRuns_StillExposesRunPaths()
    {
        var editor = JobFormatterContext.Build(BuildDefinition());

        var lastRun = Assert.IsType<Dictionary<string, object?>>(editor["LastRun"]);
        Assert.Contains(nameof(JobRunRes.State), lastRun.Keys);
        Assert.Contains(nameof(JobRunRes.FinishedTimestamp), lastRun.Keys);
    }

    /// <summary>The worker's <c>jobrun</c> bag is the other half of what resolves at run time; <c>{jobrun.parameters.emailTo}</c> is the shape that matters.</summary>
    [Fact]
    public void Build_ExposesJobRunParametersMap()
    {
        var definition = BuildDefinition();
        var editor = JobFormatterContext.Build(definition);

        var jobRun = Assert.IsType<Dictionary<string, object?>>(editor["jobrun"]);
        var parameters = Assert.IsType<Dictionary<string, object?>>(jobRun["Parameters"]);
        Assert.Contains("EmailTo", parameters.Keys);
        Assert.DoesNotContain(nameof(JobRunRes.JobRunParameters), jobRun.Keys);
    }

    private static void AssertCovers(IEnumerable<string> schedulerKeys, Dictionary<string, object?> editor)
    {
        var missing = schedulerKeys.Where(k => !editor.ContainsKey(k)).ToList();
        Assert.Empty(missing);
    }

    /// <summary>Calls the run factory's own <c>BuildTemplateData</c>, so the expectation is the real thing instead of a copy of it.</summary>
    private static IReadOnlyCollection<string> SchedulerKeys(JobInfo jobInfo, JobTriggerRes? trigger, JobRunRes? triggeredBy, JobScheduleRes? schedule)
        => JobRunRequestFactory.BuildTemplateData(jobInfo, trigger, triggeredBy, schedule).Keys;

    private static JobDefinitionRes BuildDefinition()
    {
        var definitionId = Guid.NewGuid();
        JobParameterRes[] parameters = [
            new(Guid.NewGuid(), definitionId, "EmailTo", null, LyoTypeInfo.String.FullName, null, null, true, true),
            new(Guid.NewGuid(), definitionId, "Note", null, FormatterLyoType.Template.FullName, null, null, true, false)
        ];

        JobScheduleParameterRes[] scheduleParameters = [
            new(Guid.NewGuid(), Guid.NewGuid(), "EmailTo", LyoTypeInfo.String.FullName, "to@example.com", null, null, true),
            new(Guid.NewGuid(), Guid.NewGuid(), "Disabled", LyoTypeInfo.String.FullName, "ignored", null, null, false)
        ];

        JobScheduleRes schedule = new(
            Guid.NewGuid(), definitionId, MonthFlags.None, DayFlags.None, ScheduleType.SetTimes, [new(12, 0)], null, null, null, "test schedule", true, scheduleParameters);

        JobTriggerParameterRes[] triggerParameters = [new(Guid.NewGuid(), Guid.NewGuid(), "Source", LyoTypeInfo.String.FullName, "trigger", null, null, true)];
        JobTriggerRes trigger = new(
            Guid.NewGuid(), definitionId, "RowCount", ComparisonOperatorEnum.GreaterThan, "0", null, true, null, triggerParameters, null);

        return new(definitionId, "FormatterContextDef", null, "Test", "cs", true, parameters, [schedule], [trigger], null);
    }

    private static JobRunRes BuildRun(Guid definitionId)
    {
        var runId = Guid.NewGuid();
        return new() {
            Id = runId,
            JobDefinitionId = definitionId,
            State = JobState.Finished,
            Result = JobRunResult.Success,
            CreatedTimestamp = new(2026, 8, 16, 14, 30, 0, DateTimeKind.Utc),
            JobRunParameters = [new(Guid.NewGuid(), runId, "EmailTo", LyoTypeInfo.String.FullName, "to@example.com", null, null, true)],
            JobRunResults = [new(Guid.NewGuid(), runId, "RowCount", LyoTypeInfo.Int.FullName, "42")]
        };
    }
}
