namespace Lyo.Job.Postgres.Database;

public class JobDefinition
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Type { get; set; } = null!;

    public string WorkerType { get; set; } = null!;

    public bool Enabled { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public virtual ICollection<JobParallelRestriction> JobParallelRestrictionBaseJobDefinitions { get; set; } = new List<JobParallelRestriction>();

    public virtual ICollection<JobParallelRestriction> JobParallelRestrictionOtherJobDefinitions { get; set; } = new List<JobParallelRestriction>();

    public virtual ICollection<JobParameter> JobParameters { get; set; } = new List<JobParameter>();

    public virtual ICollection<JobRun> JobRuns { get; set; } = new List<JobRun>();

    public virtual ICollection<JobSchedule> JobSchedules { get; set; } = new List<JobSchedule>();

    public virtual ICollection<JobTrigger> JobTriggerJobDefinitions { get; set; } = new List<JobTrigger>();

    public virtual ICollection<JobTrigger> JobTriggerTriggersJobDefinitions { get; set; } = new List<JobTrigger>();
}