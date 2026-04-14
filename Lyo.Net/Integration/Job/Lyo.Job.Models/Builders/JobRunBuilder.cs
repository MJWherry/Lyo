using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;

namespace Lyo.Job.Models.Builders;

public class JobRunBuilder(Guid jobDefinitionId, string createdBy, bool allowTriggers = true, Guid? triggerId = null, Guid? scheduleId = null)
{
    private readonly JobRunReq _request = new(jobDefinitionId, createdBy, allowTriggers, triggerId, scheduleId);

    public JobRunBuilder AddParameter(string key, string? value = null, string? description = null)
    {
        _request.JobRunParameters.Add(new(key, JobParameterType.String, value, description));
        return this;
    }

    public JobRunBuilder AddParameter(string key, int? value = null, string? description = null)
    {
        _request.JobRunParameters.Add(new(key, JobParameterType.Int, value.ToString(), description));
        return this;
    }

    public JobRunBuilder AddParameter(string key, JobParameterType type, string? value, string? description = null)
    {
        var parameter = new JobRunParameterReq {
            Key = key,
            Type = type,
            Value = value,
            Description = description
        };

        _request.JobRunParameters.Add(parameter);
        return this;
    }

    public JobRunBuilder AddEncryptedParameter(string key, JobParameterType type, byte[]? encryptedValue, string? description = null)
    {
        var parameter = new JobRunParameterReq {
            Key = key,
            Type = type,
            EncryptedValue = encryptedValue,
            Description = description
        };

        _request.JobRunParameters.Add(parameter);
        return this;
    }

    public JobRunReq Build() => _request;

    public static JobRunBuilder New(Guid jobDefinitionId, string createdBy) => new(jobDefinitionId, createdBy);
}