using Lyo.Common.Metadata.Records;
using Lyo.Job.Models.Request;
using Lyo.Query.Models.Enums;

namespace Lyo.Job.Models.Builders;

public class JobTriggerBuilder
{
    private readonly JobTriggerReq _trigger = new() { Enabled = true };

    public JobTriggerBuilder AddTriggerParameter(string key, LyoTypeInfo type, object? value = null, string? description = null, bool enabled = true)
    {
        _trigger.CreateTriggerParameters.Add(
            new() {
                Key = key,
                Type = type.FullName,
                Value = value is null ? null : type.ToJson(value),
                Description = description,
                Enabled = enabled
            });

        return this;
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