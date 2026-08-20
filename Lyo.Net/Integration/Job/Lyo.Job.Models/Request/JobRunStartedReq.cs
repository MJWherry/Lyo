namespace Lyo.Job.Models.Request;

/// <summary>Optional body for <c>POST /Job/Run/{id}/Started</c>. Empty body is allowed for older workers.</summary>
public sealed class JobRunStartedReq
{
    /// <summary>Worker instance that claimed this run. Snapshotted onto the run at start so history survives instance prune.</summary>
    public Guid? WorkerInstanceId { get; set; }
}
