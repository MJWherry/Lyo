using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;

namespace Lyo.Job.Models.Builders;

public class JobWorkflowBuilder
{
    private readonly JobWorkflowReq _workflow = new();

    public JobWorkflowBuilder(string name, string? description = null)
    {
        _workflow.Name = name;
        _workflow.Description = description;
    }

    public JobWorkflowBuilder Enabled(bool enabled = true)
    {
        _workflow.Enabled = enabled;
        return this;
    }

    public JobWorkflowBuilder AddStep(
        Guid jobDefinitionId,
        string stepName,
        int stepOrder,
        string? dependsOnStepIds = null,
        JobWorkflowFailurePolicy failurePolicy = JobWorkflowFailurePolicy.Stop,
        string? parametersJson = null,
        bool enabled = true)
    {
        _workflow.CreateSteps.Add(
            new() {
                JobDefinitionId = jobDefinitionId,
                StepName = stepName,
                StepOrder = stepOrder,
                DependsOnStepIds = dependsOnStepIds,
                FailurePolicy = failurePolicy,
                ParametersJson = parametersJson,
                Enabled = enabled
            });

        return this;
    }

    public JobWorkflowReq Build() => _workflow;

    public static JobWorkflowBuilder New(string name, string? description = null) => new(name, description);
}