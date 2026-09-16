using Lyo.Job.Models.Enums;

namespace Lyo.Job.Postgres.Database;

public class JobWorkflowRun
{
    public Guid Id { get; set; }

    public Guid JobWorkflowId { get; set; }

    public JobWorkflowRunState State { get; set; }

    /// <summary>Caller-supplied de-duplication key, unique per workflow. A repeated start with the same key resolves to the run created by the first one.</summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>Schedule that started this run. Null for a manual or event-driven start.</summary>
    public Guid? JobScheduleId { get; set; }

    /// <summary>The cron instant this run covers. Unique per schedule, so two schedulers racing the same slot converge on one run.</summary>
    public DateTime? ScheduledSlotUtc { get; set; }

    /// <summary>
    /// Values this run branches on, as a serialized flat string map. Resolved once at start from the caller's inputs layered over the schedule's enabled parameters, so
    /// editing a schedule parameter mid-run cannot flip a gate that already went the other way.
    /// </summary>
    public string? InputsJson { get; set; }

    public DateTime? StartedTimestamp { get; set; }

    public DateTime? FinishedTimestamp { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public virtual JobWorkflow JobWorkflow { get; set; } = null!;

    public virtual JobSchedule? JobSchedule { get; set; }

    public virtual ICollection<JobWorkflowRunStep> JobWorkflowRunSteps { get; set; } = new List<JobWorkflowRunStep>();
}