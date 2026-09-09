using Microsoft.Extensions.Options;

namespace Lyo.Job.Scheduler;

internal sealed class JobSchedulerOptionsValidator : IValidateOptions<JobSchedulerOptions>
{
    public ValidateOptionsResult Validate(string? name, JobSchedulerOptions options)
    {
        var errors = options.GetValidationErrors();
        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail($"Invalid {nameof(JobSchedulerOptions)}: {string.Join(" ", errors)}");
    }
}