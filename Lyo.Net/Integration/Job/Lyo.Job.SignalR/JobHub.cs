namespace Lyo.Job.SignalR;

/// <summary>SignalR hub for live job dashboard updates.</summary>
public sealed class JobHub : Microsoft.AspNetCore.SignalR.Hub
{
    /// <summary>Clients call this to confirm connectivity.</summary>
    public Task<string> Ping() => Task.FromResult("pong");
}

/// <summary>Payload broadcast to dashboard clients.</summary>
public sealed record JobHubEvent(string EventType, Guid? RunId, Guid? DefinitionId, string? WorkerType, DateTime TimestampUtc, string? Message);
