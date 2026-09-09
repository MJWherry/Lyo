using Lyo.Exceptions.Models;

namespace Lyo.Job.Scheduler.Tests;

public class JobWorkflowEngineOptionsTests
{
    [Fact]
    public void Validate_WhenValid_DoesNotThrow()
    {
        var options = new JobWorkflowEngineOptions { ApiBaseUrl = "https://api.example.com" };
        options.Validate();
    }

    [Fact]
    public void Validate_WhenApiBaseUrlMissing_ThrowsValidationException()
    {
        var options = new JobWorkflowEngineOptions { ApiBaseUrl = " " };
        var ex = Assert.Throws<ValidationException>(() => options.Validate());
        Assert.Contains(nameof(JobWorkflowEngineOptions.ApiBaseUrl), ex.Message, StringComparison.Ordinal);
    }
}