using System.ComponentModel.DataAnnotations;
using Lyo.Job.Models.Enums;

namespace Lyo.Job.Postgres.Database;

public class JobWorkflowStep
{
    public Guid Id { get; set; }

    public Guid JobWorkflowId { get; set; }

    public Guid JobDefinitionId { get; set; }

    [Required]
    [MaxLength(200)]
    public string StepName { get; set; } = null!;

    public int StepOrder { get; set; }

    /// <summary>Comma-separated step ids that must finish before this step may run.</summary>
    public string? DependsOnStepIds { get; set; }

    /// <summary>How the workflow continues when this step fails. Stored as a string.</summary>
    [Required]
    [MaxLength(20)]
    public string FailurePolicy { get; set; } = nameof(JobWorkflowFailurePolicy.Stop);

    public string? ParametersJson { get; set; }

    /// <summary>
    /// Serialized <c>WhereClause</c> gating this step, evaluated against the step's context bag. Each condition's field is a context path
    /// (<c>Step.Extract.Result.RowCount</c>, <c>Input.Env</c>, <c>Workflow.Name</c>) and groups combine predicates with AND/OR. Null when the step is unconditional.
    /// </summary>
    public string? ConditionJson { get; set; }

    public bool Enabled { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public virtual JobWorkflow JobWorkflow { get; set; } = null!;

    public virtual JobDefinition JobDefinition { get; set; } = null!;

    public virtual ICollection<JobWorkflowRunStep> JobWorkflowRunSteps { get; set; } = new List<JobWorkflowRunStep>();
}