namespace Lyo.Job.Postgres.Database;

public class JobRunLog
{
    public Guid Id { get; set; }

    public Guid JobRunId { get; set; }

    public string Level { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string? Context { get; set; }

    public string? StackTrace { get; set; }

    public DateTime Timestamp { get; set; }

    public virtual JobRun JobRun { get; set; } = null!;
}