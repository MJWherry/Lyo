using System.ComponentModel.DataAnnotations;

namespace Lyo.Job.Postgres.Database;

public class JobScheduleParameter
{
    public Guid Id { get; set; }

    public Guid JobScheduleId { get; set; }

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

    public virtual JobSchedule JobSchedule { get; set; } = null!;
}