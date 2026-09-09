namespace Lyo.Job.Models.Request;

/// <summary>Progress update body for the run heartbeat endpoint.</summary>
public sealed class JobRunHeartbeatReq
{
    /// <summary>Completion percent (0-100).</summary>
    public int? ProgressPercent { get; set; }

    /// <summary>Short human-readable progress text.</summary>
    public string? ProgressMessage { get; set; }
}