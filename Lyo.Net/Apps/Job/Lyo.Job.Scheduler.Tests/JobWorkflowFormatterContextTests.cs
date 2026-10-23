using Lyo.Common.Core.Identifiers;
using Lyo.Common.Metadata.Records;
using Lyo.Job.Models.Context;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Response;
using Lyo.Job.Web.Components;

namespace Lyo.Job.Scheduler.Tests;

/// <summary>
/// The step editor advertises the tokens a workflow step's parameter templates and condition gates can read, and what actually resolves is whatever
/// <see cref="JobWorkflowEngine.BuildStepScope" /> seeded. Both go through <see cref="JobContextBag.WithWorkflowStep" />, and these assert that stays true — so an engine change
/// cannot leave the editor offering paths that resolve to nothing.
/// </summary>
public class JobWorkflowFormatterContextTests
{
    [Fact]
    public void WithWorkflowStep_SeedsUpstreamRunUnderStepName()
    {
        var definition = BuildDefinition();
        var step = BuildStep("Transform", definition.Id);
        var upstreamRun = BuildRun(definition.Id);
        var bag = new JobContextBag().WithWorkflowStep(step, definition, [("Extract", upstreamRun)]);
        Assert.True(bag.TryGetValue("Step.Extract.Result.RowCount", out var rowCount));
        Assert.Equal("42", rowCount);
        Assert.True(bag.TryGetValue("Step.Extract.Parameter.EmailTo", out var _));
        Assert.Same(upstreamRun, bag["Step.Extract"]);
    }

    /// <summary>The step itself has to be readable from its own scope, otherwise a template cannot name the step it belongs to.</summary>
    [Fact]
    public void WithWorkflowStep_SeedsTheStepAndItsDefinition()
    {
        var definition = BuildDefinition();
        var step = BuildStep("Transform", definition.Id);
        var bag = new JobContextBag().WithWorkflowStep(step, definition, []);
        Assert.Same(step, bag[JobContextBag.CurrentStepKey]);
        Assert.Same(definition, bag["Definition"]);
    }

    [Fact]
    public void Build_WorkflowStepEditorContext_CoversEveryEngineKey()
    {
        var definition = BuildDefinition();
        var extract = BuildStep("Extract", definition.Id);
        var transform = BuildStep("Transform", definition.Id, extract.Id);
        var workflow = BuildWorkflow([extract, transform]);
        var workflowRun = BuildWorkflowRun(workflow, """{"Env":"prod"}""");
        var upstreamRun = BuildRun(definition.Id);
        var engine = EngineScope(workflowRun, transform, definition, [("Extract", upstreamRun)]);
        var editor = JobWorkflowFormatterContext.Build(transform, definition, workflow, ["Env"], [("Extract", definition)]);

        // Result keys are the one thing the editor cannot know: only the worker decides what a run emits, and nothing has run while authoring. Everything else the engine
        // seeds has to be offered, or the editor is advertising a vocabulary the runtime does not share.
        var missing = engine.Keys.Where(key => !key.Contains(".Result.", StringComparison.Ordinal) && !editor.ContainsKey(key)).ToList();
        Assert.Empty(missing);
        Assert.Contains("Step.Extract.Result.RowCount", engine.Keys);
    }

    /// <summary>Authoring happens before anything has run, so upstream run paths have to resolve against a placeholder or the editor offers no autocomplete at all.</summary>
    [Fact]
    public void Build_WithoutRuns_StillExposesUpstreamRunPaths()
    {
        var definition = BuildDefinition();
        var step = BuildStep("Transform", definition.Id);
        var editor = JobWorkflowFormatterContext.Build(step, definition, BuildWorkflow([step]), [], [("Extract", definition)]);
        var upstream = Assert.IsType<Dictionary<string, object?>>(editor["Step.Extract"]);
        Assert.Contains(nameof(JobRunRes.State), upstream.Keys);
        Assert.Contains(nameof(JobRunRes.FinishedTimestamp), upstream.Keys);

        // The dependency's declared parameter keys are the one nested section knowable before it runs.
        Assert.True(editor.ContainsKey("Step.Extract.Parameter.EmailTo"));
    }

    [Fact]
    public void Build_SeedsDeclaredRunInputs()
    {
        var definition = BuildDefinition();
        var step = BuildStep("Transform", definition.Id);
        var editor = JobWorkflowFormatterContext.Build(step, definition, BuildWorkflow([step]), ["Env", "Region"], []);
        Assert.True(editor.ContainsKey("Input.Env"));
        Assert.True(editor.ContainsKey("Input.Region"));
    }

    /// <summary>
    /// The step editor offers condition fields and template tokens by walking this bag into dotted paths, so what the catalog produces is literally the vocabulary the user picks
    /// from. These are the paths that made the hand-written list unnecessary.
    /// </summary>
    [Fact]
    public void Build_WalkedIntoPaths_OffersUpstreamWorkflowInputAndDefinitionFields()
    {
        var definition = BuildDefinition();
        var step = BuildStep("Transform", definition.Id);
        var context = JobWorkflowFormatterContext.Build(step, definition, BuildWorkflow([step]), ["Env"], [("Extract", definition)]);
        var paths = JobWorkflowFormatterContext.Paths(context);
        Assert.Contains("Workflow.Name", paths);
        Assert.Contains("Definition.Name", paths);
        Assert.Contains("Input.Env", paths);
        Assert.Contains("Step.Extract.State", paths);
        Assert.Contains("Step.Extract.Parameter.EmailTo", paths);
    }

    /// <summary>Calls the engine's own scope builder, so the expectation is the real thing instead of a copy of it.</summary>
    private static JobContextBag EngineScope(
        JobWorkflowRunRes workflowRun,
        JobWorkflowStepRes step,
        JobDefinitionRes definition,
        IReadOnlyList<(string StepName, JobRunRes? Run)> upstream)
    {
        var runScope = new JobContextBag().WithWorkflow(workflowRun.JobWorkflow, workflowRun).WithInputs(workflowRun.InputsJson);
        return runScope.WithWorkflowStep(step, definition, upstream);
    }

    private static JobWorkflowStepRes BuildStep(string name, Guid definitionId, Guid? dependsOn = null)
        => new(LyoGuid.CreateV7(), LyoGuid.CreateV7(), definitionId, name, 0, dependsOn?.ToString(), JobWorkflowFailurePolicy.Stop, null, true);

    private static JobWorkflowRes BuildWorkflow(IReadOnlyList<JobWorkflowStepRes> steps) => new(LyoGuid.CreateV7(), "Nightly", "Nightly pipeline", true, steps);

    private static JobWorkflowRunRes BuildWorkflowRun(JobWorkflowRes workflow, string? inputsJson)
        => new(LyoGuid.CreateV7(), workflow.Id, JobWorkflowRunState.Running, null, null, DateTime.UtcNow, [], workflow, InputsJson: inputsJson);

    private static JobDefinitionRes BuildDefinition()
    {
        var definitionId = LyoGuid.CreateV7();
        JobParameterRes[] parameters = [new(LyoGuid.CreateV7(), definitionId, "EmailTo", null, LyoTypeInfo.String.FullName, null, null, true, true)];
        return new(definitionId, "WorkflowContextDef", null, "Test", "cs", true, parameters, [], null);
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