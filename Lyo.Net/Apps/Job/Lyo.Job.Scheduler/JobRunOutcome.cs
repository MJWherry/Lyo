using Lyo.Job.Models.Enums;

namespace Lyo.Job.Scheduler;

/// <summary>How run outcomes are classified for retry scheduling, the circuit breaker, alerting, and batch aggregation, so those paths cannot drift apart.</summary>
public static class JobRunOutcome
{
    /// <summary>
    /// Whether the outcome counts as a failure. <see cref="JobRunResult.Timeout" /> does: a hung run found by the dead-job watchdog has to trip the breaker, be retried, and
    /// fail its parent batch. <see cref="JobRunResult.Cancelled" /> does not, because an operator asked for it.
    /// </summary>
    /// <param name="result">Outcome recorded on the run, or null when it never reported one.</param>
    public static bool IsFailure(JobRunResult? result) => result is JobRunResult.Failure or JobRunResult.Timeout;

    /// <summary>Whether the outcome counts as a success for circuit-breaker reset (including warnings and partial success).</summary>
    /// <param name="result">Outcome recorded on the run, or null when it never reported one.</param>
    public static bool IsSuccess(JobRunResult? result) => result is JobRunResult.Success or JobRunResult.SuccessWithWarnings or JobRunResult.PartialSuccess;
}
