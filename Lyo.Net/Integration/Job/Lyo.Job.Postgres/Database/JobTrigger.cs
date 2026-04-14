namespace Lyo.Job.Postgres.Database;

public class JobTrigger
{
    public Guid Id { get; set; }

    public Guid JobDefinitionId { get; set; }

    public Guid TriggersJobDefinitionId { get; set; }

    public string TriggerJobResultKey { get; set; } = null!;

    public string TriggerComparator { get; set; } = null!;

    public string? TriggerJobResultValue { get; set; }

    public string? Description { get; set; }

    public bool Enabled { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public virtual JobDefinition JobDefinition { get; set; } = null!;

    public virtual ICollection<JobRun> JobRuns { get; set; } = new List<JobRun>();

    public virtual ICollection<JobTriggerParameter> JobTriggerParameters { get; set; } = new List<JobTriggerParameter>();

    public virtual JobDefinition TriggersJobDefinition { get; set; } = null!;
}