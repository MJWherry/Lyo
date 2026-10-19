using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Identifiers;
using Lyo.Common.Metadata.Records;
using Lyo.Formatter;
using Lyo.Job.Models;
using Lyo.Job.Models.Context;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Response;
using Lyo.Job.Web.Components;
using Lyo.Schedule.Models;

namespace Lyo.Job.Scheduler.Tests;

/// <summary>
/// The template editor offers the keys from <see cref="JobFormatterContext" />, and the keys that actually resolve come from the scheduler's own context bag. Both are built
/// from the same <see cref="JobContextBag" /> seeders, and these assert that stays true — so a scheduler change cannot leave the editor advertising tokens that resolve to
/// nothing (or hiding ones that would work).
/// </summary>
/// <remarks>
/// These live here instead of in <c>Lyo.Job.Tests</c> because the scheduler side of the comparison needs <see cref="JobRunRequestFactory" />, and the fakes for that already
/// exist in this project.
/// </remarks>
public class JobFormatterContextTests
{
    private static readonly DateTimeOffset Frozen = new(2026, 8, 22, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Build_WithoutRuns_CoversEverySchedulerKey()
    {
        var definition = BuildDefinition();
        var schedule = definition.JobSchedules![0];

        var scheduler = SchedulerKeys(new(definition), schedule);
        var editor = JobFormatterContext.Build(definition, schedule: schedule);

        AssertCovers(scheduler, editor);
    }

    [Fact]
    public void Build_WithLatestRuns_CoversFlattenedRunKeys()
    {
        var definition = BuildDefinition();
        var schedule = definition.JobSchedules![0];
        var run = BuildRun(definition.Id);
        var scheduler = SchedulerKeys(new(definition, run, run, run), schedule);
        var editor = JobFormatterContext.Build(definition, LatestRuns(definition.Id, run), schedule);

        AssertCovers(scheduler, editor);
        Assert.True(editor.TryGetValue("LastRun.Result.RowCount", out _));
        Assert.True(editor.TryGetValue("LastRun.Parameter.EmailTo", out _));
    }

    /// <summary>Every token the editor lists has to resolve. Underscore spellings are not aliases of the dotted keys.</summary>
    [Fact]
    public void Build_AdvertisedEditorTokens_AllResolve()
    {
        var definition = BuildDefinition();
        var schedule = definition.JobSchedules![0];
        var run = BuildRun(definition.Id);
        var editor = JobFormatterContext.Build(definition, LatestRuns(definition.Id, run), schedule);

        Assert.Empty(editor.Keys.Where(key => !editor.TryGetValue(key, out _)).ToList());

        var formatter = new FormatterService(() => Frozen);
        Assert.Equal("42", formatter.Format("{LastRun.Result.RowCount}", editor));
        Assert.NotEqual("42", formatter.Format("{LastRun_Result_RowCount}", editor));
        Assert.NotEqual("to@example.com", formatter.Format("{Schedule_Parameter_EmailTo}", editor));
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

    private static JobDefinitionLatestRunsRes LatestRuns(Guid definitionId, JobRunRes run)
        => new() {
            JobDefinitionId = definitionId,
            LastRun = run,
            LastSuccessfulRun = run,
            LastFailedRun = run
        };

    private static void AssertCovers(IEnumerable<string> schedulerKeys, JobContextBag editor)
    {
        var missing = schedulerKeys.Where(k => !editor.ContainsKey(k)).ToList();
        Assert.Empty(missing);
    }

    /// <summary>Calls the run factory's own <c>BuildTemplateData</c>, so the expectation is the real thing instead of a copy of it.</summary>
    private static IReadOnlyCollection<string> SchedulerKeys(JobInfo jobInfo, JobScheduleRes? schedule) => [.. JobRunRequestFactory.BuildTemplateData(jobInfo, schedule).Keys];

    private static JobDefinitionRes BuildDefinition()
    {
        var definitionId = LyoGuid.CreateV7();
        JobParameterRes[] parameters = [
            new(LyoGuid.CreateV7(), definitionId, "EmailTo", null, LyoTypeInfo.String.FullName, null, null, true, true),
            new(LyoGuid.CreateV7(), definitionId, "Note", null, FormatterLyoType.Template.FullName, null, null, true, false)
        ];

        JobScheduleParameterRes[] scheduleParameters = [
            new(LyoGuid.CreateV7(), LyoGuid.CreateV7(), "EmailTo", LyoTypeInfo.String.FullName, "to@example.com", null, null, true),
            new(LyoGuid.CreateV7(), LyoGuid.CreateV7(), "Disabled", LyoTypeInfo.String.FullName, "ignored", null, null, false)
        ];

        JobScheduleRes schedule = new(
            LyoGuid.CreateV7(), definitionId, MonthFlags.None, DayFlags.None, ScheduleType.SetTimes, [new(12, 0)], null, null, null, "test schedule", true, scheduleParameters);

        return new(definitionId, "FormatterContextDef", null, "Test", "cs", true, parameters, [schedule], null);
    }

    private static JobRunRes BuildRun(Guid definitionId)
    {
        var runId = LyoGuid.CreateV7();
        return new() {
            Id = runId,
            JobDefinitionId = definitionId,
            State = JobState.Finished,
            Result = JobRunResult.Success,
            CreatedTimestamp = new(2026, 8, 16, 14, 30, 0, DateTimeKind.Utc),
            JobRunParameters = [new(LyoGuid.CreateV7(), runId, "EmailTo", LyoTypeInfo.String.FullName, "to@example.com", null, null, true)],
            JobRunResults = [new(LyoGuid.CreateV7(), runId, "RowCount", LyoTypeInfo.Int.FullName, "42")]
        };
    }
}
