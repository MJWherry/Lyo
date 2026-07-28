using System.ComponentModel.DataAnnotations;

namespace Lyo.Job.Postgres.Database;

public class JobWorkflow
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool Enabled { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public virtual ICollection<JobWorkflowStep> JobWorkflowSteps { get; set; } = new List<JobWorkflowStep>();

    public virtual ICollection<JobWorkflowRun> JobWorkflowRuns { get; set; } = new List<JobWorkflowRun>();
}