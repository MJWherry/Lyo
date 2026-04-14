namespace Lyo.Job.Postgres.Database;

public class JobTriggerParameter
{
    public Guid Id { get; set; }

    public Guid JobTriggerId { get; set; }

    public string? Description { get; set; }

    public string Key { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string? Value { get; set; }

    public bool Enabled { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public virtual JobTrigger JobTrigger { get; set; } = null!;
}