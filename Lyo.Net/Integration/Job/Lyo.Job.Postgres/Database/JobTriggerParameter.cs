using System.ComponentModel.DataAnnotations;

namespace Lyo.Job.Postgres.Database;

public class JobTriggerParameter
{
    public Guid Id { get; set; }

    public Guid JobTriggerId { get; set; }

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

    public bool Enabled { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public virtual JobTrigger JobTrigger { get; set; } = null!;
}