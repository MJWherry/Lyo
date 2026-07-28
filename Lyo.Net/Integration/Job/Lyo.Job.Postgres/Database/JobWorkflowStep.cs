using System.ComponentModel.DataAnnotations;
using Lyo.Job.Models.Enums;

namespace Lyo.Job.Postgres.Database;

public class JobWorkflowStep
{
    public Guid Id { get; set; }

    public Guid JobWorkflowId { get; set; }

    public Guid JobDefinitionId { get; set; }

    [Required]
    [MaxLength(100)]
    public string StepName { get; set; } = null!;

    public int StepOrder { get; set; }

    /// <summary>Comma-separated step ids that must finish before this step can run.</summary>
    public string? DependsOnStepIds { get; set; }

    /// <summary>How the workflow proceeds when this step fails. Stored as string.</summary>
    [Required]
    [MaxLength(20)]
    public string FailurePolicy { get; set; } = nameof(JobWorkflowFailurePolicy.Stop);

    public string? ParametersJson { get; set; }

    public bool Enabled { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public virtual JobWorkflow JobWorkflow { get; set; } = null!;

    public virtual JobDefinition JobDefinition { get; set; } = null!;

    public virtual ICollection<JobWorkflowRunStep> JobWorkflowRunSteps { get; set; } = new List<JobWorkflowRunStep>();
}