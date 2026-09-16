using System.Diagnostics;

namespace Lyo.Job.Models.Request;

/// <summary>
/// Body of a "start this workflow" call. Every field is optional: an empty body starts the workflow once, immediately, with no de-duplication. Supply
/// <see cref="IdempotencyKey" /> whenever the caller may retry (an at-least-once queue delivery, a UI double-click, a scheduler redelivery) so the retry resolves to the run
/// the first attempt created instead of starting the workflow twice.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class JobWorkflowStartReq
{
    /// <summary>De-duplication key, unique per workflow. A second start carrying a key an existing run already holds returns that run instead of creating a new one.</summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>Schedule this start is on behalf of. Set with <see cref="ScheduledSlotUtc" /> by the workflow scheduler.</summary>
    public Guid? JobScheduleId { get; set; }

    /// <summary>
    /// The cron instant this start covers. Together with <see cref="JobScheduleId" /> it de-duplicates independently of <see cref="IdempotencyKey" />, so two schedulers
    /// racing the same slot converge on one run.
    /// </summary>
    public DateTime? ScheduledSlotUtc { get; set; }

    /// <summary>
    /// Values this run branches on, frozen onto the run at start and readable from any step's condition or parameter template as <c>{Input.&lt;key&gt;}</c>. When
    /// <see cref="JobScheduleId" /> is set these are layered over that schedule's enabled parameters, so a manual start can override one value without redefining the rest.
    /// </summary>
    public IDictionary<string, string>? Inputs { get; set; }

    public override string ToString() => IdempotencyKey is null ? "Start" : $"Start (IdempotencyKey={IdempotencyKey})";
}
