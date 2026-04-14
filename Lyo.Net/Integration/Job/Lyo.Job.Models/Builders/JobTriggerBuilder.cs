using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Lyo.Query.Models.Enums;

namespace Lyo.Job.Models.Builders;

public class JobTriggerBuilder
{
    private readonly JobTriggerReq _trigger = new() { Enabled = true };

    public JobTriggerBuilder AddTriggerParameter(string key, JobParameterType type, object? value = null, string? description = null, bool enabled = true)
    {
        _trigger.CreateTriggerParameters.Add(
            new() {
                Key = key,
                Type = type,
                Value = value?.ToString(),
                Description = description,
                Enabled = enabled
            });

        return this;
    }

    public JobTriggerBuilder AddEmailToParameter(string email)
    {
        var currentNum = _trigger.CreateTriggerParameters.Count(i => i.Key.StartsWith(Constants.Data.JobRunParameterKey.EmailToPrefix)) + 1;
        return AddTriggerParameter($"{Constants.Data.JobRunParameterKey.EmailToPrefix}{currentNum}", JobParameterType.String, email);
    }

    public JobTriggerBuilder AddEmailCcParameter(string email)
    {
        var currentNum = _trigger.CreateTriggerParameters.Count(i => i.Key.StartsWith(Constants.Data.JobRunParameterKey.EmailCcPrefix)) + 1;
        return AddTriggerParameter($"{Constants.Data.JobRunParameterKey.EmailCcPrefix}{currentNum}", JobParameterType.String, email);
    }

    public JobTriggerBuilder AddEmailBccParameter(string email)
    {
        var currentNum = _trigger.CreateTriggerParameters.Count(i => i.Key.StartsWith(Constants.Data.JobRunParameterKey.EmailBccPrefix)) + 1;
        return AddTriggerParameter($"{Constants.Data.JobRunParameterKey.EmailBccPrefix}{currentNum}", JobParameterType.String, email);
    }

    public JobTriggerBuilder AddEmailAttachmentParameter(Guid fileId, string? fileName)
    {
        var currentNum = _trigger.CreateTriggerParameters.Count(i => i.Key.StartsWith(Constants.Data.JobRunParameterKey.FileNamePrefix)) + 1;
        if (!string.IsNullOrEmpty(fileName))
            AddTriggerParameter($"{Constants.Data.JobRunParameterKey.EmailAttachmentNamePrefix}{currentNum}", JobParameterType.String, fileId);

        return AddTriggerParameter($"{Constants.Data.JobRunParameterKey.EmailAttachmentPrefix}{currentNum}", JobParameterType.String, fileId);
    }

    public JobTriggerBuilder SetCondition(string key, ComparisonOperatorEnum comparator, string? value)
    {
        _trigger.JobResultKey = key;
        _trigger.Comparison = comparator;
        _trigger.JobResultValue = value;
        return this;
    }

    public JobTriggerBuilder SetDescription(string description)
    {
        _trigger.Description = description;
        return this;
    }

    public JobTriggerBuilder SetEnabled(bool enabled)
    {
        _trigger.Enabled = enabled;
        return this;
    }

    public JobTriggerReq Build() => _trigger;

    public static JobTriggerBuilder New() => new();
}