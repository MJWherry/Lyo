using System.ComponentModel.DataAnnotations;

namespace Lyo.Job.Postgres.Database;

/// <summary>
/// A live registration row for a running job worker process. Workers insert a row on start, heartbeat it periodically (updating <see cref="LastHeartbeatUtc" /> and
/// <see cref="InFlightCount" />), and mark it <c>Stopped</c> on graceful shutdown. <see cref="JobMaintenanceService" /> removes rows whose heartbeat has gone stale.
/// </summary>
public class JobWorkerInstance
{
    public Guid Id { get; set; }

    /// <summary>Worker type this instance consumes (matches <see cref="JobDefinition.WorkerType" />).</summary>
    [Required]
    [MaxLength(100)]
    public string WorkerType { get; set; } = null!;

    /// <summary>Machine (host) name the worker process is running on.</summary>
    [Required]
    [MaxLength(200)]
    public string MachineName { get; set; } = null!;

    /// <summary>OS process id of the worker host.</summary>
    public int ProcessId { get; set; }

    /// <summary>Lifecycle state: Running, Draining, or Stopped. Stored as string.</summary>
    [Required]
    [MaxLength(10)]
    public string State { get; set; } = null!;

    /// <summary>Number of runs currently being executed by this instance (as of the last heartbeat).</summary>
    public int InFlightCount { get; set; }

    /// <summary>UTC timestamp when the worker registered.</summary>
    public DateTime StartedTimestamp { get; set; }

    /// <summary>UTC timestamp of the last heartbeat.</summary>
    public DateTime LastHeartbeatUtc { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }
}