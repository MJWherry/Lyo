using Lyo.Common.Metadata.Records;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Response;
using Constants = Lyo.Job.Models.Constants;

namespace Lyo.Job.Scheduler.Tests;

/// <summary>
/// Batch parents inherit the worst child outcome. The regression here is <see cref="JobRunResult.Timeout" />: it once fell through the failure check, so a batch whose child
/// was killed by the dead-job watchdog reported <c>Success</c> to the parent.
/// </summary>
public class JobBatchResultAggregatorTests
{
    [Theory]
    [InlineData(JobRunResult.Timeout, JobRunResult.Failure)]
    [InlineData(JobRunResult.Failure, JobRunResult.Failure)]
    [InlineData(JobRunResult.PartialSuccess, JobRunResult.PartialSuccess)]
    [InlineData(JobRunResult.SuccessWithWarnings, JobRunResult.SuccessWithWarnings)]
    [InlineData(JobRunResult.Success, JobRunResult.Success)]
    public void Aggregate_TakesTheWorstChildOutcome(JobRunResult childResult, JobRunResult expected)
    {
        var results = JobBatchResultAggregator.Aggregate([Child(JobRunResult.Success), Child(childResult)]);
        Assert.Equal(expected.ToString(), ResultValue(results, Constants.Data.JobRunResultKey.Result));
        Assert.Equal("2", ResultValue(results, "ChildCount"));
    }

    [Fact]
    public void Aggregate_SumsCountersAndOmitsZeroes()
    {
        var results = JobBatchResultAggregator.Aggregate([
            Child(JobRunResult.Success, (Constants.Data.JobRunResultKey.CreateCount, 3)),
            Child(JobRunResult.Success, (Constants.Data.JobRunResultKey.CreateCount, 4), (Constants.Data.JobRunResultKey.UpdateCount, 1))
        ]);

        Assert.Equal("7", ResultValue(results, Constants.Data.JobRunResultKey.CreateCount));
        Assert.Equal("1", ResultValue(results, Constants.Data.JobRunResultKey.UpdateCount));
        Assert.DoesNotContain(results, r => r.Key == Constants.Data.JobRunResultKey.DeleteCount);
    }

    [Fact]
    public void Aggregate_WithNoChildren_ReportsSuccessWithZeroCount()
    {
        var results = JobBatchResultAggregator.Aggregate([]);
        Assert.Equal(nameof(JobRunResult.Success), ResultValue(results, Constants.Data.JobRunResultKey.Result));
        Assert.Equal("0", ResultValue(results, "ChildCount"));
    }

    /// <summary>Result values are stored as JSON, so a string arrives in quotes.</summary>
    private static string? ResultValue(IEnumerable<Lyo.Job.Models.Request.JobRunResultReq> results, string key)
        => results.FirstOrDefault(r => r.Key == key)?.Value?.Trim('"');

    private static JobRunRes Child(JobRunResult result, params (string Key, int Value)[] counters)
    {
        var runId = Guid.NewGuid();
        return new() {
            Id = runId,
            JobDefinitionId = Guid.NewGuid(),
            State = JobState.Finished,
            Result = result,
            CreatedTimestamp = DateTime.UtcNow,
            JobRunResults = counters.Select(c => new JobRunResultRes(Guid.NewGuid(), runId, c.Key, LyoTypeInfo.Int.FullName, c.Value.ToString())).ToList()
        };
    }
}
