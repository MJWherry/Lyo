using Lyo.Job.Models;
using Lyo.Job.Models.Enums;

namespace Lyo.Job.Scheduler.Tests;

public class JobRetryBackoffTests
{
    [Theory]
    [InlineData(10, 1, 10)]
    [InlineData(10, 2, 20)]
    [InlineData(10, 3, 30)]
    [InlineData(5, 4, 20)]
    public void ComputeBackoffSeconds_Linear_ReturnsBaseTimesAttempt(int baseSeconds, int attempt, int expected)
    {
        var result = JobRetryBackoff.ComputeBackoffSeconds(baseSeconds, attempt, JobRetryBackoffType.Linear);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(10, 0)]
    [InlineData(-5, 2)]
    public void ComputeBackoffSeconds_WhenBaseOrAttemptInvalid_ReturnsZero(int baseSeconds, int attempt)
    {
        var result = JobRetryBackoff.ComputeBackoffSeconds(baseSeconds, attempt, JobRetryBackoffType.Linear);
        Assert.Equal(0, result);
    }

    [Fact]
    public void ComputeBackoffSeconds_Exponential_AppliesJitterWithinRange()
    {
        const int baseSeconds = 20;
        const int attempt = 2;
        const int nominal = baseSeconds * (1 << (attempt - 1));
        for (var i = 0; i < 50; i++) {
            var result = JobRetryBackoff.ComputeBackoffSeconds(baseSeconds, attempt, JobRetryBackoffType.Exponential);
            Assert.InRange(result, (int)Math.Round(nominal * 0.75), (int)Math.Round(nominal * 1.25));
            Assert.True(result >= 1);
        }
    }

    [Fact]
    public void ComputeBackoffSeconds_Exponential_FirstAttemptUsesBaseWithJitter()
    {
        const int baseSeconds = 8;
        for (var i = 0; i < 50; i++) {
            var result = JobRetryBackoff.ComputeBackoffSeconds(baseSeconds, 1, JobRetryBackoffType.Exponential);
            Assert.InRange(result, (int)Math.Round(baseSeconds * 0.75), (int)Math.Round(baseSeconds * 1.25));
        }
    }
}