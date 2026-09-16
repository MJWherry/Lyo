using Lyo.Common.Core;
using Lyo.Common.Core.Identifiers;
using Lyo.Common.Metadata.Records;
using Lyo.Formatter;
using Lyo.Job.Models.Context;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Response;
using Lyo.Schedule.Models;
using Lyo.Common.Core.Enums;

namespace Lyo.Job.Tests;

/// <summary>
/// <see cref="JobContextBag" /> is the single template context the scheduler, the worker, and the parameter editor all build. These lock the parts a caller depends on: the
/// dotted keys templates resolve against, the underscore aliases that keep already-persisted templates working, and the per-step isolation a workflow DAG needs.
/// </summary>
public class JobContextBagTests
{
    private static readonly DateTimeOffset Frozen = new(2026, 8, 22, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TryGetValue_LegacyUnderscoreKey_ResolvesDottedEntry()
    {
        var bag = new JobContextBag().WithLastRuns(BuildRun(), null, null);

        // Templates already stored in JobScheduleParameter.Value use the flat underscore form. They have to keep resolving without a data migration.
        Assert.True(bag.TryGetValue("LastRun_Result_RowCount", out var value));
        Assert.Equal("42", value);
    }

    [Fact]
    public void WithInputs_SeedsInputDotKeyEntries()
    {
        var bag = new JobContextBag().WithInputs("""{"Env":"prod","Region":"eu"}""");

        Assert.True(bag.TryGetValue("Input.Env", out var env));
        Assert.Equal("prod", env);
        Assert.True(bag.TryGetValue("Input.Region", out var region));
        Assert.Equal("eu", region);
    }

    [Fact]
    public void WithInputs_MalformedJson_SeedsNothing()
    {
        // A hand-edited or truncated inputs column must not stop the engine from starting the run's steps.
        var bag = new JobContextBag().WithInputs("{ not json");

        Assert.False(bag.TryGetValue("Input.Env", out _));
    }

    [Fact]
    public void TryGetValue_LegacyUnderscoreKeyWhoseTailContainsUnderscores_ResolvesDottedEntry()
    {
        var bag = new JobContextBag().WithLastRuns(BuildRun("Row_Count"), null, null);

        // Only the prefix segments are separators: "Row_Count" is one result key, not two path steps.
        Assert.True(bag.TryGetValue("LastRun_Result_Row_Count", out var value));
        Assert.Equal("42", value);
    }

    [Fact]
    public void WithLastRuns_RunResults_ExposeDottedPath()
    {
        var bag = new JobContextBag().WithLastRuns(BuildRun(), null, null);

        Assert.Equal("42", Format("{LastRun.Result.RowCount}", bag));
        Assert.Equal("to@example.com", Format("{LastRun.Parameter.EmailTo}", bag));
    }

    [Fact]
    public void WithSchedule_DisabledParameter_IsNotExposed()
    {
        var bag = new JobContextBag().WithSchedule(BuildSchedule());

        Assert.True(bag.TryGetValue("Schedule.Parameter.EmailTo", out _));
        Assert.False(bag.TryGetValue("Schedule.Parameter.Disabled", out _));
        Assert.False(bag.TryGetValue("Schedule_Parameter_Disabled", out _));
    }

    [Fact]
    public void CreateStepScope_SiblingScopes_DoNotSeeEachOthersWrites()
    {
        var workflowId = LyoGuid.CreateV7();
        var run = new JobContextBag().WithLastRuns(BuildRun(), null, null);
        var extract = run.CreateStepScope(BuildStep(workflowId, "Extract"));
        var load = run.CreateStepScope(BuildStep(workflowId, "Load"));

        extract.WithUpstreamRun("Seed", BuildRun());

        Assert.False(load.TryGetValue("Step.Seed.Result.RowCount", out _));
        Assert.True(extract.TryGetValue("Step.Seed.Result.RowCount", out _));

        // Both still see everything the run-level bag seeded.
        Assert.True(load.TryGetValue("LastRun.Result.RowCount", out _));
    }

    [Fact]
    public void CreateStepScope_NamesTheCurrentStep()
    {
        var scope = new JobContextBag().CreateStepScope(BuildStep(LyoGuid.CreateV7(), "Load"));

        Assert.Equal("Load", Format("{CurrentStep.StepName}", scope));
    }

    [Fact]
    public void WithUpstreamRun_TwoSteps_KeyedByStepName()
    {
        var bag = new JobContextBag().WithUpstreamRun("Extract", BuildRun(value: "7")).WithUpstreamRun("Load", BuildRun(value: "9"));

        Assert.Equal("7", Format("{Step.Extract.Result.RowCount}", bag));
        Assert.Equal("9", Format("{Step.Load.Result.RowCount}", bag));
    }

    [Fact]
    public void Freeze_ThenSet_Throws()
    {
        var frozen = new JobContextBag().WithLastRuns(BuildRun(), null, null).Freeze();

        Assert.Throws<ContextBagException>(() => frozen.Set("Anything", "value"));
        Assert.True(frozen.TryGetValue("LastRun.Result.RowCount", out _));
    }

    [Fact]
    public void WithRun_RunParameters_ResolveThroughTheJobRunBag()
    {
        var bag = new JobContextBag().WithRun(BuildRun());

        // The worker's existing token shape. Changing it would break every string parameter already authored against it.
        Assert.Equal("to@example.com", Format("{jobrun.Parameters.EmailTo}", bag));
        Assert.Equal(nameof(JobState.Finished), Format("{jobrun.State}", bag));
    }

    private static string Format(string template, JobContextBag bag) => new FormatterService(() => Frozen).Format(template, bag);

    private static JobWorkflowStepRes BuildStep(Guid workflowId, string name)
        => new(LyoGuid.CreateV7(), workflowId, LyoGuid.CreateV7(), name, 0, null, JobWorkflowFailurePolicy.Stop, null, true);

    private static JobScheduleRes BuildSchedule()
    {
        var scheduleId = LyoGuid.CreateV7();
        JobScheduleParameterRes[] parameters = [
            new(LyoGuid.CreateV7(), scheduleId, "EmailTo", LyoTypeInfo.String.FullName, "to@example.com", null, null, true),
            new(LyoGuid.CreateV7(), scheduleId, "Disabled", LyoTypeInfo.String.FullName, "ignored", null, null, false)
        ];

        return new(scheduleId, LyoGuid.CreateV7(), MonthFlags.None, DayFlags.None, ScheduleType.SetTimes, [new(12, 0)], null, null, null, "schedule", true, parameters);
    }

    private static JobRunRes BuildRun(string resultKey = "RowCount", string value = "42")
    {
        var runId = LyoGuid.CreateV7();
        return new() {
            Id = runId,
            JobDefinitionId = LyoGuid.CreateV7(),
            State = JobState.Finished,
            Result = JobRunResult.Success,
            CreatedTimestamp = new(2026, 8, 16, 14, 30, 0, DateTimeKind.Utc),
            JobRunParameters = [new(LyoGuid.CreateV7(), runId, "EmailTo", LyoTypeInfo.String.FullName, "to@example.com", null, null, true)],
            JobRunResults = [new(LyoGuid.CreateV7(), runId, resultKey, LyoTypeInfo.Int.FullName, value)]
        };
    }
}
