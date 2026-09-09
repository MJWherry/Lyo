using Lyo.Job.Models.Enums;

namespace Lyo.Job.Scheduler.Tests;

/// <summary>
/// One classification feeds retry scheduling, the circuit breaker, alerting, and batch aggregation. These pin the two decisions that were inconsistent before it existed:
/// a timeout is a failure, and a cancellation is not.
/// </summary>
public class JobRunOutcomeTests
{
    [Theory]
    [InlineData(JobRunResult.Failure, true)]
    [InlineData(JobRunResult.Timeout, true)]
    [InlineData(JobRunResult.Cancelled, false)]
    [InlineData(JobRunResult.Skipped, false)]
    [InlineData(JobRunResult.Unknown, false)]
    [InlineData(JobRunResult.Success, false)]
    [InlineData(JobRunResult.SuccessWithWarnings, false)]
    [InlineData(JobRunResult.PartialSuccess, false)]
    [InlineData(null, false)]
    public void IsFailure_ClassifiesEveryOutcome(JobRunResult? result, bool expected) => Assert.Equal(expected, JobRunOutcome.IsFailure(result));

    [Theory]
    [InlineData(JobRunResult.Success, true)]
    [InlineData(JobRunResult.SuccessWithWarnings, true)]
    [InlineData(JobRunResult.PartialSuccess, true)]
    [InlineData(JobRunResult.Skipped, false)]
    [InlineData(JobRunResult.Timeout, false)]
    [InlineData(JobRunResult.Cancelled, false)]
    [InlineData(null, false)]
    public void IsSuccess_ClassifiesEveryOutcome(JobRunResult? result, bool expected) => Assert.Equal(expected, JobRunOutcome.IsSuccess(result));

    /// <summary>A result cannot be both, and the two together cannot silently claim an unknown outcome is fine.</summary>
    [Fact]
    public void IsFailureAndIsSuccess_AreMutuallyExclusive()
    {
        foreach (var result in Enum.GetValues<JobRunResult>())
            Assert.False(JobRunOutcome.IsFailure(result) && JobRunOutcome.IsSuccess(result), $"{result} classified as both");
    }
}
