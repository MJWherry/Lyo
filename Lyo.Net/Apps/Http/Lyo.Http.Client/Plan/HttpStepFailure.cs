namespace Lyo.Http.Client.Plan;

/// <summary>What to do when a plan step fails (non-success and not an allowed status).</summary>
public enum HttpStepFailure
{
    /// <summary>Abort the plan.</summary>
    Stop = 0,

    /// <summary>Record the failure and continue.</summary>
    Continue = 1
}
