namespace Lyo.Job.Postgres.Database;

public class JobScheduleParameter
{
    public Guid Id { get; set; }

    public Guid JobScheduleId { get; set; }

    public string? Description { get; set; }

    public string Key { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string? Value { get; set; }

    public bool Enabled { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public virtual JobSchedule JobSchedule { get; set; } = null!;
}