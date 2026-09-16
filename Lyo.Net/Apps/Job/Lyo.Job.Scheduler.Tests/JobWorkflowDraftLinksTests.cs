using System.Text.Json;
using Lyo.Job.Models.Builders;
using Lyo.Job.Web.Components;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;

namespace Lyo.Job.Scheduler.Tests;

/// <summary>
/// Authoring links a step by a draft id, not by its name, so renaming the step does not leave dependents pointing at a name that no longer exists. The runtime still reads
/// <c>Step.{name}.*</c>, so a rename has to rewrite those paths in the same edit.
/// </summary>
public class JobWorkflowDraftLinksTests
{
    private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [Fact]
    public void Rename_KeepsDependencyIds()
    {
        var extract = new JobWorkflowStepDraft { StepName = "Extract", JobDefinitionId = Guid.NewGuid() };
        var load = new JobWorkflowStepDraft { StepName = "Load", JobDefinitionId = Guid.NewGuid(), DependsOn = [extract.Id] };
        List<JobWorkflowStepDraft> steps = [extract, load];

        JobWorkflowDraftLinks.Rename(steps, extract, "Sync Twilio");

        Assert.Equal("Sync Twilio", extract.StepName);
        Assert.Equal(extract.Id, Assert.Single(load.DependsOn));

        var req = JobWorkflowBuilder.New("Nightly")
            .AddStep(extract.StepName, extract.JobDefinitionId)
            .AddStep(load.StepName, load.JobDefinitionId, JobWorkflowDraftLinks.DependencyNames(steps, load))
            .Build();

        var extractId = req.CreateSteps.Single(s => s.StepName == "Sync Twilio").Id;
        Assert.Equal(extractId.ToString(), req.CreateSteps.Single(s => s.StepName == "Load").DependsOnStepIds);
    }

    [Fact]
    public void Rename_RewritesStepPaths()
    {
        var extract = new JobWorkflowStepDraft { StepName = "Extract", JobDefinitionId = Guid.NewGuid() };
        var load = new JobWorkflowStepDraft {
            StepName = "Load",
            JobDefinitionId = Guid.NewGuid(),
            DependsOn = [extract.Id],
            Condition = JobWorkflowStepCondition.When("Extract", "RowCount", ComparisonOperatorEnum.GreaterThan, "0"),
            Parameters = new() { ["Rows"] = "{Step.Extract.Result.RowCount}" }
        };

        JobWorkflowDraftLinks.Rename([extract, load], extract, "Sync Twilio");

        var condition = Assert.IsType<ConditionClause>(load.Condition);
        Assert.Equal("Step.Sync Twilio.Result.RowCount", condition.Field);
        Assert.Equal("{Step.Sync Twilio.Result.RowCount}", load.Parameters["Rows"]);
    }

    /// <summary>Drafts saved before links were ids still name their dependencies. Loading resolves those names once so the open workflow is not dropped.</summary>
    [Fact]
    public void Load_LegacyNameDependencies_ResolvesToIds()
    {
        const string json = """
            {"steps":[{"stepName":"Extract","dependsOn":[]},{"stepName":"Load","dependsOn":["Extract"]}]}
            """;

        var state = JobWorkflowDraftLinks.Load(json, CamelCase);
        var extract = state.Steps.Single(s => s.StepName == "Extract");
        var load = state.Steps.Single(s => s.StepName == "Load");

        Assert.NotEqual(Guid.Empty, extract.Id);
        Assert.Equal(extract.Id, Assert.Single(load.DependsOn));
    }
}
