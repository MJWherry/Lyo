namespace Lyo.Job.Postgres.Database;

public class JobParallelRestriction
{
    public Guid Id { get; set; }

    public Guid BaseJobDefinitionId { get; set; }

    public Guid OtherJobDefinitionId { get; set; }

    public string? Description { get; set; }

    public bool Enabled { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public virtual JobDefinition BaseJobDefinition { get; set; } = null!;

    public virtual JobDefinition OtherJobDefinition { get; set; } = null!;
}