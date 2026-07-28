using Lyo.Exceptions.Models;

namespace Lyo.Job.Scheduler;

/// <summary>Options for <see cref="JobWorkflowEngine" />.</summary>
public sealed class JobWorkflowEngineOptions
{
    public const string SectionName = "JobWorkflowEngine";

    public required string ApiBaseUrl { get; set; }

    public string CreatedBy { get; set; } = "WorkflowEngine";

    public IReadOnlyList<string> GetValidationErrors()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(ApiBaseUrl))
            errors.Add($"{nameof(ApiBaseUrl)} is required.");

        return errors;
    }

    public void Validate()
    {
        var errors = GetValidationErrors();
        if (errors.Count > 0)
            throw new ValidationException($"Invalid {nameof(JobWorkflowEngineOptions)}: {string.Join(" ", errors)}");
    }
}