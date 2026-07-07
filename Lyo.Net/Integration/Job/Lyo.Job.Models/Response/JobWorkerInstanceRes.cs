using Lyo.Job.Models.Enums;

namespace Lyo.Job.Models.Response;

/// <summary>A registered (live) worker instance.</summary>
public sealed record JobWorkerInstanceRes(
    Guid Id,
    string WorkerType,
    string MachineName,
    int ProcessId,
    JobWorkerInstanceState State,
    int InFlightCount,
    DateTime StartedTimestamp,
    DateTime LastHeartbeatUtc);
