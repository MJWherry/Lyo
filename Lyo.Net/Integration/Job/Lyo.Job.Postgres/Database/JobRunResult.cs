using System.ComponentModel.DataAnnotations;

namespace Lyo.Job.Postgres.Database;

public class JobRunResult
{
    public Guid Id { get; set; }

    public Guid JobRunId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Key { get; set; } = null!;

    [Required]
    [MaxLength(15)]
    public string Type { get; set; } = null!;

    [MaxLength(16_384)]
    public string? Value { get; set; }

    public virtual JobRun JobRun { get; set; } = null!;
}