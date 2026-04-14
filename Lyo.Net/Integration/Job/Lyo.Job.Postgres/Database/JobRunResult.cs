namespace Lyo.Job.Postgres.Database;

public class JobRunResult
{
    public Guid Id { get; set; }

    public Guid JobRunId { get; set; }

    public string Key { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string? Value { get; set; }

    public virtual JobRun JobRun { get; set; } = null!;
}