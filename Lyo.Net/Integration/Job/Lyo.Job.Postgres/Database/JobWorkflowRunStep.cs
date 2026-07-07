using Lyo.Job.Models.Enums;

namespace Lyo.Job.Postgres.Database;

public class JobWorkflowRunStep
{
    public Guid Id { get; set; }

    public Guid JobWorkflowRunId { get; set; }

    public Guid JobWorkflowStepId { get; set; }

    public Guid? JobRunId { get; set; }

    public JobWorkflowStepState State { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public virtual JobWorkflowRun JobWorkflowRun { get; set; } = null!;

    public virtual JobWorkflowStep JobWorkflowStep { get; set; } = null!;

    public virtual JobRun? JobRun { get; set; }
}
