using System.ComponentModel.DataAnnotations;

namespace Lyo.Job.Postgres.Database;

public class JobRunLog
{
    public Guid Id { get; set; }

    public Guid JobRunId { get; set; }

    [Required]
    [MaxLength(13)]
    public string Level { get; set; } = null!;

    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = null!;

    [MaxLength(16_384)]
    public string? Context { get; set; }

    [MaxLength(16384)]
    public string? StackTrace { get; set; }

    public DateTime Timestamp { get; set; }

    public virtual JobRun JobRun { get; set; } = null!;
}