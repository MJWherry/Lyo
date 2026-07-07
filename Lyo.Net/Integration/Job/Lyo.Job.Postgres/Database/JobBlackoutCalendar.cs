using System.ComponentModel.DataAnnotations;

namespace Lyo.Job.Postgres.Database;

/// <summary>
/// A reusable set of blackout windows — time ranges when linked schedules must not fire (or must defer). Attach to a <see cref="JobSchedule" /> via
/// <see cref="JobSchedule.JobBlackoutCalendarId" />.
/// </summary>
public class JobBlackoutCalendar
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

    public virtual ICollection<JobBlackoutWindow> JobBlackoutWindows { get; set; } = new List<JobBlackoutWindow>();

    public virtual ICollection<JobSchedule> JobSchedules { get; set; } = new List<JobSchedule>();
}
