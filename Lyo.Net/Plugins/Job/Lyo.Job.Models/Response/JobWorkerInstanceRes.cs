using Lyo.Job.Models.Enums;

namespace Lyo.Job.Models.Response;

/// <summary>A registered (live) worker instance, including host metadata reported when it registered.</summary>
public sealed record JobWorkerInstanceRes
{
    /// <summary>Instance id assigned at register.</summary>
    public Guid Id { get; init; }

    /// <summary>Worker type this instance pulls from.</summary>
    public string WorkerType { get; init; } = "";

    /// <summary>Machine (host) name of the worker process.</summary>
    public string MachineName { get; init; } = "";

    /// <summary>OS process id of the worker host process.</summary>
    public int ProcessId { get; init; }

    /// <summary>Lifecycle state of this instance.</summary>
    public JobWorkerInstanceState State { get; init; }

    /// <summary>How many runs this instance is executing right now.</summary>
    public int InFlightCount { get; init; }

    /// <summary>UTC time when the worker registered.</summary>
    public DateTime StartedTimestamp { get; init; }

    /// <summary>UTC time of the last heartbeat.</summary>
    public DateTime LastHeartbeatUtc { get; init; }

    /// <summary>When the row was first inserted.</summary>
    public DateTime CreatedTimestamp { get; init; }

    /// <summary>When the row was last patched (heartbeat or a stop).</summary>
    public DateTime? UpdatedTimestamp { get; init; }

    /// <summary>Built-in system info, queue subscriptions, and extras from the host. See <see cref="Lyo.Job.Models.Constants.WorkerMetadata"/>.</summary>
    public IReadOnlyDictionary<string, string?>? Metadata { get; init; }
}
