using System.ComponentModel.DataAnnotations;

namespace Lyo.Job.Postgres.Database;

public class JobRunParameter
{
    public Guid Id { get; set; }

    public Guid JobRunId { get; set; }

    [MaxLength(100)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string Key { get; set; } = null!;

    [Required]
    [MaxLength(15)]
    public string Type { get; set; } = null!;

    [MaxLength(300)]
    public string? Value { get; set; }

    public byte[]? EncryptedValue { get; set; }

    public virtual JobRun JobRun { get; set; } = null!;
}