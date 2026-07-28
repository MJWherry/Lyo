using Lyo.Job.Models.Enums;

namespace Lyo.Job.Postgres.Database;

public class JobWorkflowRun
{
    public Guid Id { get; set; }

    public Guid JobWorkflowId { get; set; }

    public JobWorkflowRunState State { get; set; }

    public DateTime? StartedTimestamp { get; set; }

    public DateTime? FinishedTimestamp { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public virtual JobWorkflow JobWorkflow { get; set; } = null!;

    public virtual ICollection<JobWorkflowRunStep> JobWorkflowRunSteps { get; set; } = new List<JobWorkflowRunStep>();
}