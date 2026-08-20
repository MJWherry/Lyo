using Lyo.Job.Models.Enums;

namespace Lyo.Job.Models.Response;

/// <summary>A registered (live) worker instance, including host metadata reported at registration.</summary>
public sealed record JobWorkerInstanceRes
{
    /// <summary>Instance id assigned on register.</summary>
    public Guid Id { get; init; }

    /// <summary>Worker type this instance consumes.</summary>
    public string WorkerType { get; init; } = "";

    /// <summary>Machine (host) name the worker process is running on.</summary>
    public string MachineName { get; init; } = "";

    /// <summary>OS process id of the worker host.</summary>
    public int ProcessId { get; init; }

    /// <summary>Lifecycle state of the instance.</summary>
    public JobWorkerInstanceState State { get; init; }

    /// <summary>Number of runs currently being executed by this instance.</summary>
    public int InFlightCount { get; init; }

    /// <summary>UTC timestamp when the worker registered.</summary>
    public DateTime StartedTimestamp { get; init; }

    /// <summary>UTC timestamp of the last heartbeat.</summary>
    public DateTime LastHeartbeatUtc { get; init; }

    /// <summary>When the row was inserted.</summary>
    public DateTime CreatedTimestamp { get; init; }

    /// <summary>When the row was last patched (heartbeat or stop).</summary>
    public DateTime? UpdatedTimestamp { get; init; }

    /// <summary>Built-in system info, queue subscriptions, and host-supplied extras. See <see cref="Lyo.Job.Models.Constants.WorkerMetadata"/>.</summary>
    public IReadOnlyDictionary<string, string?>? Metadata { get; init; }
}
