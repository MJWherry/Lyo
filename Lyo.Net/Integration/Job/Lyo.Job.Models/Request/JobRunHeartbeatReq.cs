namespace Lyo.Job.Models.Request;

/// <summary>Progress update payload for the run heartbeat endpoint.</summary>
public sealed class JobRunHeartbeatReq
{
    /// <summary>Completion percentage (0-100).</summary>
    public int? ProgressPercent { get; set; }

    /// <summary>Short human-readable progress message.</summary>
    public string? ProgressMessage { get; set; }
}
