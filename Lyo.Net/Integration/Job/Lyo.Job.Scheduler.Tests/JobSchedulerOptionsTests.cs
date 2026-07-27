using Lyo.Exceptions.Models;
using Lyo.Job.Scheduler;

namespace Lyo.Job.Scheduler.Tests;

public class JobSchedulerOptionsTests
{
    [Fact]
    public void GetValidationErrors_WhenValid_ReturnsEmpty()
    {
        var options = CreateValidOptions();
        Assert.Empty(options.GetValidationErrors());
    }

    [Fact]
    public void GetValidationErrors_WhenApiBaseUrlMissing_ReturnsError()
    {
        var options = CreateValidOptions();
        options.ApiBaseUrl = "  ";

        var errors = options.GetValidationErrors();

        Assert.Contains(errors, e => e.Contains(nameof(JobSchedulerOptions.ApiBaseUrl), StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetValidationErrors_WhenDefinitionRefreshIntervalInvalid_ReturnsError(int interval)
    {
        var options = CreateValidOptions();
        options.DefinitionRefreshIntervalSeconds = interval;

        var errors = options.GetValidationErrors();

        Assert.Contains(errors, e => e.Contains(nameof(JobSchedulerOptions.DefinitionRefreshIntervalSeconds), StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void GetValidationErrors_WhenScheduleCheckIntervalInvalid_ReturnsError(int interval)
    {
        var options = CreateValidOptions();
        options.ScheduleCheckIntervalSeconds = interval;

        var errors = options.GetValidationErrors();

        Assert.Contains(errors, e => e.Contains(nameof(JobSchedulerOptions.ScheduleCheckIntervalSeconds), StringComparison.Ordinal));
    }

    [Fact]
    public void GetValidationErrors_WhenMisfireLookbackNegative_ReturnsError()
    {
        var options = CreateValidOptions();
        options.MisfireLookbackMinutes = -1;

        var errors = options.GetValidationErrors();

        Assert.Contains(errors, e => e.Contains(nameof(JobSchedulerOptions.MisfireLookbackMinutes), StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WhenInvalid_ThrowsValidationException()
    {
        var options = CreateValidOptions();
        options.ApiBaseUrl = "";

        var ex = Assert.Throws<ValidationException>(() => options.Validate());
        Assert.Contains(nameof(JobSchedulerOptions), ex.Message, StringComparison.Ordinal);
    }

    private static JobSchedulerOptions CreateValidOptions() => new() {
        ApiBaseUrl = "https://api.example.com",
        DefinitionRefreshIntervalSeconds = 30,
        ScheduleCheckIntervalSeconds = 10,
        MisfireLookbackMinutes = 1440
    };
}
