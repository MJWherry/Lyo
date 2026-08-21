namespace Lyo.Job.Models.Request;

/// <summary>Optional body for <c>POST /Job/Run/{id}/Started</c>. Empty body is allowed for older workers.</summary>
public sealed class JobRunStartedReq
{
    /// <summary>Worker instance that claimed this run. Snapshotted onto the run at start so history survives instance prune.</summary>
    public Guid? WorkerInstanceId { get; set; }

    /// <summary>Machine name of the claiming process. Used when the instance row is missing or not yet registered.</summary>
    public string? MachineName { get; set; }

    /// <summary>OS process id of the claiming process. Used when the instance row is missing or not yet registered.</summary>
    public int? ProcessId { get; set; }
}
