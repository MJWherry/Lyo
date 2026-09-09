using Lyo.Common.Metadata.Records;
using Lyo.Job.Models.Request;

namespace Lyo.Job.Models.Builders;

public class JobRunBuilder(Guid jobDefinitionId, string createdBy, bool allowTriggers = true, Guid? triggerId = null, Guid? scheduleId = null)
{
    private readonly JobRunReq _request = new(jobDefinitionId, createdBy, allowTriggers, triggerId, scheduleId);

    public JobRunBuilder AddParameter(string key, string? value = null, string? description = null)
    {
        _request.JobRunParameters.Add(new(key, LyoTypeInfo.String, value is null ? null : LyoTypeInfo.String.ToJson(value), description));
        return this;
    }

    public JobRunBuilder AddParameter(string key, int? value = null, string? description = null)
    {
        _request.JobRunParameters.Add(new(key, LyoTypeInfo.Int, value is null ? null : LyoTypeInfo.Int.ToJson(value), description));
        return this;
    }

    public JobRunBuilder AddParameter(string key, LyoTypeInfo type, string? value, string? description = null)
    {
        _request.JobRunParameters.Add(
            new() {
                Key = key,
                Type = type.FullName,
                Value = value,
                Description = description
            });
        return this;
    }

    public JobRunBuilder AddEncryptedParameter(string key, LyoTypeInfo type, byte[]? encryptedValue, string? description = null)
    {
        _request.JobRunParameters.Add(
            new() {
                Key = key,
                Type = type.FullName,
                EncryptedValue = encryptedValue,
                Description = description
            });
        return this;
    }

    public JobRunBuilder WithIdempotencyKey(string idempotencyKey)
    {
        _request.IdempotencyKey = idempotencyKey;
        return this;
    }

    public JobRunBuilder AsDryRun(bool dryRun = true)
    {
        _request.DryRun = dryRun;
        return this;
    }

    public JobRunBuilder WithTraceId(string traceId)
    {
        _request.TraceId = traceId;
        return this;
    }

    public JobRunBuilder WithParent(Guid parentJobRunId, int? batchIndex = null, int? batchTotal = null)
    {
        _request.ParentJobRunId = parentJobRunId;
        _request.BatchIndex = batchIndex;
        _request.BatchTotal = batchTotal;
        return this;
    }

    public JobRunReq Build() => _request;

    public static JobRunBuilder New(Guid jobDefinitionId, string createdBy) => new(jobDefinitionId, createdBy);
}