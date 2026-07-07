using System.ComponentModel.DataAnnotations;

namespace Lyo.Job.Postgres.Database;

public class JobCalendar
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool Enabled { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public virtual ICollection<JobCalendarWindow> JobCalendarWindows { get; set; } = new List<JobCalendarWindow>();

    public virtual ICollection<JobSchedule> JobSchedules { get; set; } = new List<JobSchedule>();
}
